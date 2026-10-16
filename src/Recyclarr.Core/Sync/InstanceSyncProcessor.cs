using Autofac;
using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.Logging;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync.Results;
using Serilog.Context;
using SemanticInstanceResult = Recyclarr.Sync.Results.SyncInstanceResult;

namespace Recyclarr.Sync;

internal class InstanceSyncProcessor(
    ILogger log,
    IServiceConfiguration config,
    ServiceAgnosticCapabilityEnforcer enforcer,
    ILifetimeScope instanceScope
)
{
    public async Task<SemanticInstanceResult> Process(
        ISyncSettings settings,
        InstanceExecutionState state,
        CancellationToken ct
    )
    {
        using var _ = LogContext.PushProperty(LogProperty.Scope, config.InstanceName);
        PipelinePlan? plan = null;

        try
        {
            log.Information(
                "Processing {Server} server {Name}",
                config.ServiceType,
                config.InstanceName
            );

            // Planning and pipelines depend on version-selected gateways, which exist only in the
            // scope created after detection succeeds. The instance scope owns its disposal so
            // cleanup faults surface where the orchestrator isolates them, not as processing
            // failures.
            var detected = await enforcer.Check(config, ct);
            var detectedScope = instanceScope.BeginDetectedServiceScope(detected);
            instanceScope.Disposer.AddInstanceForAsyncDisposal(detectedScope);

            plan = detectedScope.Resolve<PlanBuilder>().Build();
            state.RetainPlanningOutcomes(plan.PlanningOutcomes);
            var result = await detectedScope
                .Resolve<IPipelineExecutor>()
                .Execute(settings, plan, state.PipelineBuffer, ct);
            return new SemanticInstanceResult(
                config.InstanceName,
                config.ServiceType,
                result,
                planningOutcomes: plan.PlanningOutcomes
            );
        }
        catch (Exception e)
        {
            // Unwrap DI exceptions to get the actual cause
            var actual = e is DependencyResolutionException { InnerException: { } inner }
                ? inner
                : e;

            var operationalFailure = OperationalFailureClassifier.Classify(actual);
            if (operationalFailure is null)
            {
                throw;
            }

            log.Debug(actual, "Instance sync error (details logged for diagnostics)");
            return new SemanticInstanceResult(
                config.InstanceName,
                config.ServiceType,
                state.PipelineBuffer.Results,
                failure: operationalFailure,
                planningOutcomes: plan?.PlanningOutcomes
            );
        }
    }
}
