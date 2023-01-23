using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.QualitySize.PipelinePhases;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Pipelines.QualitySize;

public interface IQualitySizePipelinePhases
{
    QualitySizeGuidePhase GuidePhase { get; }
    Lazy<QualitySizePreviewPhase> PreviewPhase { get; }
    QualitySizeApiFetchPhase ApiFetchPhase { get; }
    QualitySizeTransactionPhase TransactionPhase { get; }
    QualitySizeApiPersistencePhase ApiPersistencePhase { get; }
}

public class QualitySizeSyncPipeline : ISyncPipeline
{
    private readonly ILogger _log;
    private readonly IQualitySizePipelinePhases _phases;

    public QualitySizeSyncPipeline(ILogger log, IQualitySizePipelinePhases phases)
    {
        _log = log;
        _phases = phases;
    }

    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var selectedQuality = _phases.GuidePhase.Execute(config);
        if (selectedQuality is null)
        {
            _log.Debug("No quality definition to process");
            return;
        }

        if (settings.Preview)
        {
            _phases.PreviewPhase.Value.Execute(selectedQuality);
            return;
        }

        var serviceData = await _phases.ApiFetchPhase.Execute(config);
        var transactions = _phases.TransactionPhase.Execute(selectedQuality.Qualities, serviceData);
        await _phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
