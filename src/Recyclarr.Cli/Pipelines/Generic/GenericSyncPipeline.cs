using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Generic;

public class GenericSyncPipeline<TContext>(
    ILogger log,
    GenericPipelinePhases<TContext> phases,
    IServiceConfiguration config
) : ISyncPipeline
    where TContext : IPipelineContext, new()
{
    public async Task Execute(ISyncSettings settings, CancellationToken ct)
    {
        var context = new TContext();

        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);

        if (!context.SupportedServiceTypes.Contains(config.ServiceType))
        {
            log.Debug("Skipping this pipeline because it does not support service type {Service}", config.ServiceType);
            return;
        }

        await phases.ConfigPhase.Execute(context, ct);
        if (phases.LogPhase.LogConfigPhaseAndExitIfNeeded(context))
        {
            return;
        }

        await phases.ApiFetchPhase.Execute(context, ct);
        phases.TransactionPhase.Execute(context);

        phases.LogPhase.LogTransactionNotices(context);

        if (settings.Preview)
        {
            phases.PreviewPhase.Execute(context);
            return;
        }

        await phases.ApiPersistencePhase.Execute(context, ct);
        phases.LogPhase.LogPersistenceResults(context);
    }
}
