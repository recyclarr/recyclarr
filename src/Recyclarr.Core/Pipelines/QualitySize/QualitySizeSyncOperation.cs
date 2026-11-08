using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

// The quality definition gateway is resolved lazily because Sonarr v5 has none: sizes live on
// quality profiles there (the quality profile planner assigns them), so the quality size planner
// produces no plan and this operation never runs.
internal class QualitySizeSyncOperation(
    ILogger log,
    Lazy<IQualityDefinitionService> api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : SyncOperation<PlannedQualitySizes, QualitySizeComputeResult>
{
    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => new QualitySizePipelineResult(0, 0, [], []).WithStatus(status, blockedBy);

    protected override async Task<QualitySizeComputeResult> Compute(
        PlannedQualitySizes planned,
        IReadOnlyList<SyncOutcome> planOutcomes,
        CancellationToken ct
    )
    {
        var outcomes = BuildPlanOutcomes(planOutcomes, out var incompleteResources);
        if (planned.Qualities.Count == 0)
        {
            var emptyResult = new QualitySizePipelineResult(0, incompleteResources, outcomes, []);
            return new QualitySizeComputeResult([], null, planned.Type, emptyResult);
        }

        var serverQuality = await api.Value.GetQualityDefinitions(ct);
        var limits = await limitFactory.Create(config.ServiceType, ct);
        var updatedItems = new List<UpdatedQualityItem>();
        var resources = new List<PlannedQualitySizeResource>();
        var completedResources = 0;

        foreach (var plannedQuality in planned.Qualities)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.QualityName == plannedQuality.Quality
            );
            if (serverEntry == null)
            {
                outcomes.Add(new QualitySizeServiceQualityNotFoundOutcome(plannedQuality.Quality));
                continue;
            }

            var resolved = QualitySizeCalculator.ResolveValues(
                plannedQuality,
                planned.PreferredRatio,
                limits
            );
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
            completedResources++;
            resources.Add(BuildResource(item, limits));
        }

        var result = new QualitySizePipelineResult(
            completedResources,
            incompleteResources,
            outcomes,
            resources
        );
        return new QualitySizeComputeResult(updatedItems, limits, planned.Type, result);
    }

    protected override async Task Persist(
        QualitySizeComputeResult computeResult,
        CancellationToken ct
    )
    {
        var limits = computeResult.Limits;
        if (limits is null)
        {
            return;
        }

        // Build the list of API items that differ from what the server already has
        var itemsToUpdate = computeResult
            .Items.Where(x => x.IsDifferent)
            .Select(x => x.BuildUpdatedItem(limits))
            .ToList();

        if (itemsToUpdate.Count == 0)
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                computeResult.QualityDefinitionType
            );
            return;
        }

        await api.Value.UpdateQualityDefinitions(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            computeResult.QualityDefinitionType
        );
    }

    private static List<QualitySizeOutcome> BuildPlanOutcomes(
        IReadOnlyList<SyncOutcome> planOutcomes,
        out int incompleteResources
    )
    {
        var outcomes = new List<QualitySizeOutcome>();
        incompleteResources = 0;

        foreach (var outcome in planOutcomes)
        {
            var mapped = QualitySizeOutcomeMapper.Map(outcome);
            if (mapped is null)
            {
                continue;
            }

            outcomes.Add(mapped);
            if (QualitySizeOutcomeMapper.IsIncomplete(mapped))
            {
                incompleteResources++;
            }
        }

        return outcomes;
    }

    private static PlannedQualitySizeResource BuildResource(
        UpdatedQualityItem item,
        QualityItemLimits limits
    ) =>
        PlannedQualitySizeResource.Compared(
            item.Quality,
            (Numeric(item.ServerItem.MinSize), Numeric(item.Min)),
            (
                ToSemanticValue(item.ServerItem.PreferredSize, limits.PreferredLimit),
                ToSemanticValue(item.Preferred, limits.PreferredLimit)
            ),
            (
                ToSemanticValue(item.ServerItem.MaxSize, limits.MaxLimit),
                ToSemanticValue(item.Max, limits.MaxLimit)
            )
        );

    private static QualitySizeValue.Numeric Numeric(decimal value) => new(value);

    private static QualitySizeValue ToSemanticValue(decimal? value, decimal unlimitedBoundary) =>
        value is null
            ? new QualitySizeValue.Unlimited()
            : ToSemanticValue(value.Value, unlimitedBoundary);

    private static QualitySizeValue ToSemanticValue(decimal value, decimal unlimitedBoundary) =>
        value >= unlimitedBoundary ? new QualitySizeValue.Unlimited() : Numeric(value);

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
