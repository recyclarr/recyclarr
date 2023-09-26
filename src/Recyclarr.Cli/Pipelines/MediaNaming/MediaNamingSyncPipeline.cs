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

public class MediaNamingSyncPipeline : ISyncPipeline
{
    private readonly IMediaNamingPipelinePhases _phases;

    public MediaNamingSyncPipeline(IMediaNamingPipelinePhases phases)
    {
        _phases = phases;
    }

    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var processedNaming = await _phases.ConfigPhase.Execute(config);
        if (_phases.Logger.LogConfigPhaseAndExitIfNeeded(processedNaming))
        {
            return;
        }

        var serviceData = await _phases.ApiFetchPhase.Execute(config);

        var transactions = _phases.TransactionPhase.Execute(serviceData, processedNaming);

        if (settings.Preview)
        {
            _phases.PreviewPhase.Execute(transactions);
            return;
        }

        await _phases.ApiPersistencePhase.Execute(config, transactions);
        _phases.Logger.LogPersistenceResults(serviceData, transactions);
    }
}
