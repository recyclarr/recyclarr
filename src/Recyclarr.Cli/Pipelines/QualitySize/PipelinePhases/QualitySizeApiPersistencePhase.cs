using Recyclarr.Notifications;
using Recyclarr.ServarrApi.QualityDefinition;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiPersistencePhase(
    ILogger log,
    IQualityDefinitionApiService api,
    NotificationEmitter notificationEmitter
) : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        var sizeData = context.TransactionOutput;
        if (sizeData.Count == 0)
        {
            log.Debug("No size data available to persist; skipping API call");
            return PipelineFlow.Terminate;
        }

        await api.UpdateQualityDefinition(sizeData, ct);
        LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }

    private void LogPersistenceResults(QualitySizePipelineContext context)
    {
        var qualityDefinitionName = context.QualitySizeType;

        var totalCount = context.TransactionOutput.Count;
        if (totalCount > 0)
        {
            log.Information(
                "Total of {Count} sizes were synced for quality definition {Name}",
                totalCount,
                qualityDefinitionName
            );
            notificationEmitter.SendStatistic("Quality Sizes Synced", totalCount);
        }
        else
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                qualityDefinitionName
            );
        }
    }
}
