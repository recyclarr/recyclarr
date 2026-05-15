using System.Diagnostics.CodeAnalysis;
using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Serilog.Context;

namespace Recyclarr.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class InstanceSyncProcessor(
    ILogger log,
    IServiceConfiguration config,
    IInstancePublisher instancePublisher,
    PlanBuilder planBuilder,
    IPipelineExecutor pipelines,
    ServiceAgnosticCapabilityEnforcer enforcer,
    IEnumerable<IExceptionStrategy> strategies
)
{
    public async Task<ExitStatus> Process(ISyncSettings settings, CancellationToken ct)
    {
        using var _ = LogContext.PushProperty(LogProperty.Scope, config.InstanceName);

        try
        {
            log.Information(
                "Processing {Server} server {Name}",
                config.ServiceType,
                config.InstanceName
            );

            await enforcer.Check(config, ct);

            var plan = planBuilder.Build();
            var result = await pipelines.Execute(settings, plan, instancePublisher, ct);
            return result == PipelineResult.Failed ? ExitStatus.Failed : ExitStatus.Succeeded;
        }
        catch (Exception e)
        {
            // Unwrap DI exceptions to get the actual cause
            var actual = e is DependencyResolutionException { InnerException: { } inner }
                ? inner
                : e;

            foreach (var strategy in strategies)
            {
                var messages = await strategy.HandleAsync(actual);
                if (messages is null)
                {
                    continue;
                }

                foreach (var message in messages)
                {
                    instancePublisher.AddError(message);
                }

                log.Debug(actual, "Instance sync error (details logged for diagnostics)");
                pipelines.InterruptAll(instancePublisher);
                return ExitStatus.Failed;
            }

            throw;
        }
    }
}
