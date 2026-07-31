using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync.Results;
using Refit;
using Serilog.Context;
using SemanticInstanceResult = Recyclarr.Sync.Results.SyncInstanceResult;

namespace Recyclarr.Sync;

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
    public async Task<SemanticInstanceResult> Process(
        ISyncSettings settings,
        PipelineExecutionBuffer buffer,
        CancellationToken ct
    )
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
            var result = await pipelines.Execute(settings, plan, instancePublisher, buffer, ct);
            return new SemanticInstanceResult(config.InstanceName, config.ServiceType, result);
        }
        catch (Exception e)
        {
            // Unwrap DI exceptions to get the actual cause
            var actual = e is DependencyResolutionException { InnerException: { } inner }
                ? inner
                : e;

            var operationalFailure = MapOperationalFailure(actual);
            if (operationalFailure is null)
            {
                throw;
            }

            await PublishDiagnostic(actual);
            log.Debug(actual, "Instance sync error (details logged for diagnostics)");
            pipelines.InterruptAll(instancePublisher);
            return new SemanticInstanceResult(
                config.InstanceName,
                config.ServiceType,
                buffer.Results,
                operationalFailure
            );
        }
    }

    private async Task PublishDiagnostic(Exception exception)
    {
        foreach (var strategy in strategies)
        {
            var failure = await strategy.HandleAsync(exception);
            if (failure is null)
            {
                continue;
            }

            instancePublisher.Add(failure);
            return;
        }
    }

    private static OperationalFailure? MapOperationalFailure(Exception exception) =>
        exception switch
        {
            ServiceIncompatibilityException => new ServiceIncompatibleFailure(),
            SyncState.SyncStateUnavailableException => new SyncStateUnavailableFailure(),
            ApiRequestException => new ServiceUnavailableFailure(),
            HttpRequestException => new ServiceUnavailableFailure(),
            ApiException { StatusCode: System.Net.HttpStatusCode.Unauthorized } =>
                new ServiceUnauthenticatedFailure(),
            ApiException { StatusCode: System.Net.HttpStatusCode.Forbidden } =>
                new ServiceUnauthorizedFailure(),
            ApiException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } =>
                new ServiceRateLimitedFailure(),
            ApiException { StatusCode: >= System.Net.HttpStatusCode.InternalServerError } =>
                new ServiceUnavailableFailure(),
            _ => null,
        };
}
