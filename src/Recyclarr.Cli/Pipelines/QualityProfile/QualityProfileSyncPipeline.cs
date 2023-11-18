using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Config.Models;

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

public class QualityProfileSyncPipeline(ILogger log, IQualityProfilePipelinePhases phases) : ISyncPipeline
{
    public async Task Execute(ISyncSettings settings, IServiceConfiguration config)
    {
        var guideData = phases.ConfigPhase.Execute(config);
        if (!guideData.Any())
        {
            log.Debug("No quality profiles to process");
            return;
        }

        var serviceData = await phases.ApiFetchPhase.Execute(config);
        var transactions = phases.TransactionPhase.Execute(guideData, serviceData);

        phases.NoticePhase.Execute(transactions);

        if (settings.Preview)
        {
            phases.PreviewPhase.Value.Execute(transactions);
            return;
        }

        await phases.ApiPersistencePhase.Execute(config, transactions);
    }
}
