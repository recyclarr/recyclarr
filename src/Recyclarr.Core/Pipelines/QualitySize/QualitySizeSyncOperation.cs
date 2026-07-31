using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

internal class QualitySizeSyncOperation(
    ILogger log,
    IQualityDefinitionService api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : SyncOperation<QualitySizeComputeResult>
{
    public override PipelineType Type => PipelineType.QualitySize;
    public override string Description => "Quality Definition";

    public override bool ShouldSkip(PipelinePlan plan) => !plan.QualitySizesAvailable;

    protected override async Task<QualitySizeComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var serverQuality = await api.GetQualityDefinitions(ct);
        var limits = await limitFactory.Create(config.ServiceType, ct);

        var planned = plan.QualitySizes;
        var updatedItems = new List<UpdatedQualityItem>();

        foreach (var plannedQuality in planned.Qualities)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.QualityName == plannedQuality.Quality
            );
            if (serverEntry == null)
            {
                publisher.AddWarning(
                    $"Server lacks quality definition for {plannedQuality.Quality}; it will be skipped"
                );
                continue;
            }

            var resolved = ResolveValues(plannedQuality, planned.PreferredRatio, limits);
            var item = new UpdatedQualityItem
            {
                Quality = plannedQuality.Quality,
                Min = resolved.Min,
                Max = resolved.Max,
                Preferred = resolved.Preferred,
                IsDifferent = IsDifferent(serverEntry, resolved, limits),
                ServerItem = serverEntry,
            };

            log.Debug(
                "Processed Quality {Name}: "
                    + "[IsDifferent: {IsDifferent}] "
                    + "[Min: {Min1}, {Min2}] "
                    + "[Max: {Max1}, {Max2} ({MaxLimit})] "
                    + "[Preferred: {Preferred1}, {Preferred2} ({PreferredLimit})]",
                item.Quality,
                item.IsDifferent,
                serverEntry.MinSize,
                item.Min,
                serverEntry.MaxSize,
                item.Max,
                limits.MaxLimit,
                serverEntry.PreferredSize,
                item.Preferred,
                limits.PreferredLimit
            );

            updatedItems.Add(item);
        }

        return new QualitySizeComputeResult(updatedItems, limits, planned.Type);
    }

    protected override async Task Persist(
        QualitySizeComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        // Build the list of API items that differ from what the server already has
        var itemsToUpdate = computeResult
            .Items.Where(x => x.IsDifferent)
            .Select(x => x.BuildUpdatedItem(computeResult.Limits))
            .ToList();

        if (itemsToUpdate.Count == 0)
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                computeResult.QualityDefinitionType
            );
            publisher.SetStatus(PipelineProgressStatus.Succeeded, 0);
            return;
        }

        await api.UpdateQualityDefinitions(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            computeResult.QualityDefinitionType
        );
        publisher.SetStatus(PipelineProgressStatus.Succeeded, itemsToUpdate.Count);
    }

    private static (decimal Min, decimal Max, decimal Preferred) ResolveValues(
        PlannedQualityItem planned,
        decimal? preferredRatio,
        QualityItemLimits limits
    )
    {
        var min = planned.Min;
        var max = Math.Min(planned.Max ?? limits.MaxLimit, limits.MaxLimit);
        var preferred = Math.Min(planned.Preferred ?? limits.PreferredLimit, limits.PreferredLimit);

        if (preferredRatio is not null)
        {
            var cappedMax = Math.Min(max, limits.PreferredLimit);
            preferred = Math.Round(min + (cappedMax - min) * preferredRatio.Value, decimals: 1);
        }

        return (min, max, preferred);
    }

    private static bool IsDifferent(
        QualityDefinitionItem server,
        (decimal Min, decimal Max, decimal Preferred) resolved,
        QualityItemLimits limits
    )
    {
        if (server.MinSize != resolved.Min)
        {
            return true;
        }

        var serverMax = server.MaxSize ?? limits.MaxLimit;
        if (serverMax != resolved.Max)
        {
            return true;
        }

        var serverPreferred = server.PreferredSize ?? limits.PreferredLimit;
        return serverPreferred != resolved.Preferred;
    }
}
