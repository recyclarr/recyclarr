using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Generic;

public class GenericSyncPipeline<TContext>(GenericPipelinePhases<TContext> phases) : ISyncPipeline
    where TContext : new()
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var context = new TContext();

        await phases.ConfigPhase.Execute(context, config);
        if (phases.LogPhase.LogConfigPhaseAndExitIfNeeded(context))
        {
            return;
        }

        await phases.ApiFetchPhase.Execute(context, config);
        phases.TransactionPhase.Execute(context);

        if (settings.Preview)
        {
            phases.PreviewPhase.Execute(context);
            return;
        }

        await phases.ApiPersistencePhase.Execute(context, config);
        phases.LogPhase.LogPersistenceResults(context);
    }
}
