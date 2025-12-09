using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiPersistencePhase(
    ILogger log,
    IQualityDefinitionApiService api,
    ISyncEventPublisher eventPublisher
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
            eventPublisher.AddCompletionCount(0);
            return PipelineFlow.Terminate;
        }

        await api.UpdateQualityDefinition(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            context.QualityDefinitionType
        );
        eventPublisher.AddCompletionCount(itemsToUpdate.Count);

        return PipelineFlow.Continue;
    }
}
