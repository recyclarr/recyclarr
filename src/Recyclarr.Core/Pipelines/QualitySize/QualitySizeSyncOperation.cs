using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualitySize;

internal record QualitySizePreviewData(
    IReadOnlyCollection<UpdatedQualityItem> Items,
    QualityItemLimits Limits,
    string QualityDefinitionType
);

internal class QualitySizeSyncOperation(
    ILogger log,
    IQualityDefinitionService api,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config,
    IEnumerable<IPreviewRenderer<QualitySizePreviewData>> previewRenderers
) : ISyncOperation
{
    private IReadOnlyList<QualityDefinitionItem> _apiFetchOutput = null!;
    private QualityItemLimits _limits = null!;
    private string _qualityDefinitionType = null!;
    private IReadOnlyCollection<UpdatedQualityItem> _transactionOutput = [];

    public PipelineType Type => PipelineType.QualitySize;
    public string Description => "Quality Definition";
    public IReadOnlyList<PipelineType> Dependencies => [];

    public bool ShouldSkip(PipelinePlan plan, SupportedServices serviceType) =>
        !plan.QualitySizesAvailable;

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        // Fetch phase
        _apiFetchOutput = await api.GetQualityDefinitions(ct);
        _limits = await limitFactory.Create(config.ServiceType, ct);

        // Transaction phase
        var planned = plan.QualitySizes;
        var limits = _limits;
        var serverQuality = _apiFetchOutput;

        var updatedItems = new List<UpdatedQualityItem>();
        foreach (var plannedQuality in planned.Qualities)
        {
            var serverEntry = serverQuality.FirstOrDefault(q =>
                q.QualityName == plannedQuality.Quality
            );
            if (serverEntry == null)
            {
                var message =
                    $"Server lacks quality definition for {plannedQuality.Quality}; it will be skipped";
                publisher.AddWarning(message);
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

        _transactionOutput = updatedItems;
        _qualityDefinitionType = planned.Type;
    }

    public async Task Persist(IPipelinePublisher publisher, CancellationToken ct)
    {
        var itemsToUpdate = _transactionOutput
            .Where(x => x.IsDifferent)
            .Select(x => x.BuildUpdatedItem(_limits))
            .ToList();

        if (itemsToUpdate.Count == 0)
        {
            log.Information(
                "All sizes for quality definition {Name} are already up to date!",
                _qualityDefinitionType
            );
            publisher.SetStatus(PipelineProgressStatus.Succeeded, 0);
            return;
        }

        await api.UpdateQualityDefinitions(itemsToUpdate, ct);

        log.Information(
            "Total of {Count} sizes were synced for quality definition {Name}",
            itemsToUpdate.Count,
            _qualityDefinitionType
        );
        publisher.SetStatus(PipelineProgressStatus.Succeeded, itemsToUpdate.Count);
    }

    public void RenderPreview(string instanceName)
    {
        var renderer = previewRenderers.FirstOrDefault();
        renderer?.Render(
            Description,
            instanceName,
            new QualitySizePreviewData(_transactionOutput, _limits, _qualityDefinitionType)
        );
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
