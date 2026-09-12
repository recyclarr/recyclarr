using Recyclarr.Config.Models;
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

    public static ValueChangeResponse<T> MapValue<T>(ValueDelta<T> value) =>
        new(value.Current, value.Desired);

    public static QualitySizeValueResponse MapQualitySize(QualitySizeValue value) =>
        value switch
        {
            QualitySizeValue.Numeric x => new QualitySizeValueResponse(QualitySizeKind.Numeric)
            {
                Value = x.Value,
            },
            QualitySizeValue.Unlimited => new QualitySizeValueResponse(QualitySizeKind.Unlimited),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        };

    public static ValueChangeResponse<QualitySizeValueResponse> MapQualitySizeValue(
        ValueDelta<QualitySizeValue> value
    ) => new(MapQualitySize(value.Current), MapQualitySize(value.Desired));
}
