using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Generic;

public class GenericSyncPipeline<TContext>(ILogger log, GenericPipelinePhases<TContext> phases) : ISyncPipeline
    where TContext : IPipelineContext, new()
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var context = new TContext();
        if (!context.SupportedServiceTypes.Contains(config.ServiceType))
        {
            log.Debug("Skipping {Description} because it does not support service type {Service}",
                context.PipelineDescription, config.ServiceType);
        }

        await phases.ConfigPhase.Execute(context, config);
        if (phases.LogPhase.LogConfigPhaseAndExitIfNeeded(context))
        {
            return;
        }

        await phases.ApiFetchPhase.Execute(context, config);
        phases.TransactionPhase.Execute(context);

        phases.LogPhase.LogTransactionNotices(context);

        if (settings.Preview)
        {
            phases.PreviewPhase.Execute(context);
            return;
        }

        await phases.ApiPersistencePhase.Execute(context, config);
        phases.LogPhase.LogPersistenceResults(context);
    }
}
