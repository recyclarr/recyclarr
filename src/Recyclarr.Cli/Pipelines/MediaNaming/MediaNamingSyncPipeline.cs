using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public interface IMediaNamingPipelinePhases
{
    MediaNamingConfigPhase ConfigPhase { get; }
    MediaNamingPhaseLogger Logger { get; }
    MediaNamingApiFetchPhase ApiFetchPhase { get; }
    MediaNamingTransactionPhase TransactionPhase { get; }
    MediaNamingPreviewPhase PreviewPhase { get; }
    MediaNamingApiPersistencePhase ApiPersistencePhase { get; }
}

public class MediaNamingSyncPipeline(IMediaNamingPipelinePhases phases) : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var processedNaming = await phases.ConfigPhase.Execute(config);
        if (phases.Logger.LogConfigPhaseAndExitIfNeeded(processedNaming))
        {
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);

        var transactions = phases.TransactionPhase.Execute(serviceData, processedNaming);

        if (settings.Preview)
        {
            phases.PreviewPhase.Execute(transactions);
            return;
        }

        await phases.ApiPersistencePhase.Execute(config, transactions);
        phases.Logger.LogPersistenceResults(serviceData, transactions);
    }
}
