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
        var itemsToUpdate = context
            .TransactionOutput.Where(x => x.IsDifferent)
            .Select(x => x.BuildApiItem(context.Limits))
            .ToList();

        if (itemsToUpdate.Count == 0)
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                context.QualityDefinitionType
            );
            return PipelineFlow.Terminate;
        }

        await api.UpdateQualityDefinition(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            context.QualityDefinitionType
        );
        notificationEmitter.SendStatistic("Quality Sizes Synced", itemsToUpdate.Count);

        return PipelineFlow.Continue;
    }
}
