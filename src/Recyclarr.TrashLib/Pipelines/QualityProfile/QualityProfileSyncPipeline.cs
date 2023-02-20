using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Pipelines.QualityProfile;

public interface IQualityProfilePipelinePhases
{
    QualityProfileConfigPhase ConfigPhase { get; }
    QualityProfileApiFetchPhase ApiFetchPhase { get; }
    QualityProfileTransactionPhase TransactionPhase { get; }
    Lazy<QualityProfilePreviewPhase> PreviewPhase { get; }
    QualityProfileApiPersistencePhase ApiPersistencePhase { get; }
}

public class QualityProfileSyncPipeline : ISyncPipeline
{
    private readonly ILogger _log;
    private readonly IQualityProfilePipelinePhases _phases;

    public QualityProfileSyncPipeline(ILogger log, IQualityProfilePipelinePhases phases)
    {
        _log = log;
        _phases = phases;
    }

    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var guideData = _phases.ConfigPhase.Execute(config);
        if (!guideData.Any())
        {
            _log.Debug("No quality profiles to process");
            return;
        }

        var serviceData = await _phases.ApiFetchPhase.Execute(config);
        var transactions = _phases.TransactionPhase.Execute(guideData, serviceData);

        if (settings.Preview)
        {
            _phases.PreviewPhase.Value.Execute(transactions);
            return;
        }

        await _phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
