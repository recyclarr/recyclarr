using System.ComponentModel;

namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum QualitySizeKind
{
    [Description("A size limit; `value` is present.")]
    Numeric,

    [Description("No size limit; `value` is absent.")]
    Unlimited,
}

internal sealed record QualitySizeValueResponse
{
    public required QualitySizeKind Kind { get; init; }

    /// <summary>The size in megabytes per minute of runtime.</summary>
    public decimal? Value { get; init; }
}

/// <summary>The size limits Recyclarr manages on one quality.</summary>
internal sealed record QualitySizeItemResponse
{
    public required ResourceAction Action { get; init; }
    public required string Quality { get; init; }
    public required PlannedValueResponse<QualitySizeValueResponse> Minimum { get; init; }
    public required PlannedValueResponse<QualitySizeValueResponse> Preferred { get; init; }
    public required PlannedValueResponse<QualitySizeValueResponse> Maximum { get; init; }
}

internal sealed record QualitySizePipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }

    /// <summary>
    /// Every quality the configuration sizes, changed or not. Qualities the service does not have
    /// are absent and reported as outcomes.
    /// </summary>
    public required IReadOnlyList<QualitySizeItemResponse> Items { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
