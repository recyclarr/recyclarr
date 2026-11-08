using Recyclarr.Config.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Results;

internal static class ResultValueMapper
{
    public static void EnsureAllMapped(string contract, int sourceCount, params int[] mappedCounts)
    {
        if (mappedCounts.Sum() != sourceCount)
        {
            throw new InvalidOperationException($"Unsupported {contract} variant");
        }
    }

    public static SyncCompletionStatus MapCompletionStatus(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => SyncCompletionStatus.Succeeded,
            SyncResultStatus.Partial => SyncCompletionStatus.Partial,
            SyncResultStatus.Failed => SyncCompletionStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    public static PipelineStatus MapStatus(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => PipelineStatus.Succeeded,
            SyncResultStatus.Partial => PipelineStatus.Partial,
            SyncResultStatus.Failed => PipelineStatus.Failed,
            SyncResultStatus.Blocked => PipelineStatus.Blocked,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    public static BlockingPipeline? MapBlockedBy(PipelineType? type) =>
        type switch
        {
            null => null,
            PipelineType.CustomFormat => BlockingPipeline.CustomFormat,
            PipelineType.QualityProfile => BlockingPipeline.QualityProfile,
            PipelineType.QualitySize => BlockingPipeline.QualitySize,
            PipelineType.MediaNaming => BlockingPipeline.MediaNaming,
            PipelineType.MediaManagement => BlockingPipeline.MediaManagement,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static ResourceAction MapAction(PlannedAction action) =>
        action switch
        {
            PlannedAction.Create => ResourceAction.Create,
            PlannedAction.Update => ResourceAction.Update,
            PlannedAction.Unchanged => ResourceAction.Unchanged,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    public static PlannedValueResponse<T> MapValue<T>(PlannedValue<T> value) =>
        MapValue(value, x => x);

    // Value types map to their nullable form so a created resource's current value can be null.
    public static PlannedValueResponse<T?> MapNullableValue<T>(PlannedValue<T> value)
        where T : struct => MapValue(value, x => (T?)x);

    public static PlannedValueResponse<TResponse> MapValue<T, TResponse>(
        PlannedValue<T> value,
        Func<T, TResponse> map
    ) =>
        new(
            map(value.Desired),
            value.Current is { } current ? map(current.Value) : default,
            value.Changed
        );

    public static QualitySizeValueResponse MapQualitySize(QualitySizeValue value) =>
        value switch
        {
            QualitySizeValue.Numeric x => new QualitySizeValueResponse
            {
                Kind = QualitySizeKind.Numeric,
                Value = x.Value,
            },
            QualitySizeValue.Unlimited => new QualitySizeValueResponse
            {
                Kind = QualitySizeKind.Unlimited,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        };

    public static QualitySizeItemResponse MapQualitySizeItem(PlannedQualitySizeResource size) =>
        new()
        {
            Action = MapAction(size.Action),
            Quality = size.Quality,
            Minimum = MapValue(size.Minimum, MapQualitySize),
            Preferred = MapValue(size.Preferred, MapQualitySize),
            Maximum = MapValue(size.Maximum, MapQualitySize),
        };
}
