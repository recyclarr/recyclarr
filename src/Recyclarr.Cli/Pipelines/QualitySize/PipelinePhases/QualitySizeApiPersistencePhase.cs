using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

internal class QualitySizeApiPersistencePhase(ILogger log, IQualityDefinitionService api)
    : IPipelinePhase<QualitySizePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualitySizePipelineContext context,
        CancellationToken ct
    )
    {
        var itemsToUpdate = context
            .TransactionOutput.Where(x => x.IsDifferent)
            .Select(x => x.BuildUpdatedItem(context.Limits))
            .ToList();

        if (itemsToUpdate.Count == 0)
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                context.QualityDefinitionType
            );
            context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, 0);
            return PipelineFlow.Terminate;
        }

        await api.UpdateQualityDefinitions(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            context.QualityDefinitionType
        );
        context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, itemsToUpdate.Count);

        return PipelineFlow.Continue;
    }
}
