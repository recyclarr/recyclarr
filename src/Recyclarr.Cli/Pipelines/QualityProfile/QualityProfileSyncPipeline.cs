using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public interface IQualityProfilePipelinePhases
{
    QualityProfileConfigPhase ConfigPhase { get; }
    QualityProfileApiFetchPhase ApiFetchPhase { get; }
    QualityProfileTransactionPhase TransactionPhase { get; }
    Lazy<QualityProfilePreviewPhase> PreviewPhase { get; }
    QualityProfileApiPersistencePhase ApiPersistencePhase { get; }
    QualityProfileNoticePhase NoticePhase { get; }
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

        _phases.NoticePhase.Execute(transactions);

        if (settings.Preview)
        {
            _phases.PreviewPhase.Value.Execute(transactions);
            return;
        }

        await _phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
