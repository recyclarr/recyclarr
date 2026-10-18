using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

// The quality definition gateway is resolved lazily because Sonarr v5 has none: sizes live on
// quality profiles there (the quality profile planner assigns them), so plan.QualitySizes is
// never set and ShouldSkip keeps this
// operation from touching the gateway.
internal class QualitySizeSyncOperation(
    ILogger log,
    Lazy<IQualityDefinitionService> api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : SyncOperation<QualitySizeComputeResult>
{
    public override PipelineType Type => PipelineType.QualitySize;
    public override string Description => "Quality Definition";

    protected override PipelineResult CreateEmptyResult(
        SyncResultStatus status,
        PipelineType? blockedBy
    ) => new QualitySizePipelineResult(0, 0, [], []).WithStatus(status, blockedBy);

    public override bool ShouldSkip(PipelinePlan plan) => !plan.QualitySizesAvailable;

    protected override async Task<QualitySizeComputeResult> Compute(
        PipelinePlan plan,
        CancellationToken ct
    )
    {
        var planned = plan.QualitySizes;
        var outcomes = BuildPlanOutcomes(plan, out var incompleteResources);
        if (planned.Qualities.Count == 0)
        {
            var emptyResult = new QualitySizePipelineResult(0, incompleteResources, outcomes, []);
            return new QualitySizeComputeResult([], null, planned.Type, emptyResult);
        }

        var serverQuality = await api.Value.GetQualityDefinitions(ct);
        var limits = await limitFactory.Create(config.ServiceType, ct);
        var updatedItems = new List<UpdatedQualityItem>();
        var deltas = new List<QualitySizeDelta>();
        var completedResources = 0;

        foreach (var plannedQuality in planned.Qualities)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.QualityName == plannedQuality.Quality
            );
            if (serverEntry == null)
            {
                outcomes.Add(new QualitySizeServiceQualityNotFoundOutcome(plannedQuality.Quality));
                incompleteResources++;
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

            var delta = BuildDelta(item, limits);
            if (delta is not null)
            {
                deltas.Add(delta);
            }
        }

        var result = new QualitySizePipelineResult(
            completedResources,
            incompleteResources,
            outcomes,
            deltas
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
        PipelinePlan plan,
        out int incompleteResources
    )
    {
        var outcomes = new List<QualitySizeOutcome>();
        incompleteResources = 0;

        foreach (var outcome in plan.Outcomes)
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

    private static QualitySizeDelta? BuildDelta(UpdatedQualityItem item, QualityItemLimits limits)
    {
        var components = new List<QualitySizeUpdateComponent>();
        AddChangedValue(
            components,
            Numeric(item.ServerItem.MinSize),
            Numeric(item.Min),
            value => new QualitySizeMinimumChanged(value)
        );
        AddChangedValue(
            components,
            ToSemanticValue(item.ServerItem.PreferredSize, limits.PreferredLimit),
            ToSemanticValue(item.Preferred, limits.PreferredLimit),
            value => new QualitySizePreferredChanged(value)
        );
        AddChangedValue(
            components,
            ToSemanticValue(item.ServerItem.MaxSize, limits.MaxLimit),
            ToSemanticValue(item.Max, limits.MaxLimit),
            value => new QualitySizeMaximumChanged(value)
        );

        return components.Count > 0 ? new QualitySizeDelta(item.Quality, components) : null;
    }

    private static void AddChangedValue(
        List<QualitySizeUpdateComponent> components,
        QualitySizeValue current,
        QualitySizeValue desired,
        Func<ValueDelta<QualitySizeValue>, QualitySizeUpdateComponent> createComponent
    )
    {
        if (current == desired)
        {
            return;
        }

        components.Add(createComponent(new ValueDelta<QualitySizeValue>(current, desired)));
    }

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
