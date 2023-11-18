using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.QualitySize;

public interface IQualitySizePipelinePhases
{
    QualitySizeGuidePhase GuidePhase { get; }
    Lazy<QualitySizePreviewPhase> PreviewPhase { get; }
    QualitySizeApiFetchPhase ApiFetchPhase { get; }
    QualitySizeTransactionPhase TransactionPhase { get; }
    QualitySizeApiPersistencePhase ApiPersistencePhase { get; }
}

public class QualitySizeSyncPipeline(ILogger log, IQualitySizePipelinePhases phases) : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var selectedQuality = phases.GuidePhase.Execute(config);
        if (selectedQuality is null)
        {
            log.Debug("No quality definition to process");
            return;
        }

        if (settings.Preview)
        {
            phases.PreviewPhase.Value.Execute(selectedQuality);
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);
        var transactions = phases.TransactionPhase.Execute(selectedQuality.Qualities, serviceData);
        await phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
