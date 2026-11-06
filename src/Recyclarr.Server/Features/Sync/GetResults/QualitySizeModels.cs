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

internal sealed record QualitySizeUpdateResponse
{
    public required string Quality { get; init; }
    public ValueChangeResponse<QualitySizeValueResponse>? Minimum { get; init; }
    public ValueChangeResponse<QualitySizeValueResponse>? Preferred { get; init; }
    public ValueChangeResponse<QualitySizeValueResponse>? Maximum { get; init; }
}

/// <summary>The size limits Recyclarr assigns to one quality.</summary>
internal sealed record QualitySizeResponse
{
    public required string Quality { get; init; }
    public required QualitySizeValueResponse Minimum { get; init; }
    public required QualitySizeValueResponse Preferred { get; init; }
    public required QualitySizeValueResponse Maximum { get; init; }
}

internal sealed record QualitySizePipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }
    public required IReadOnlyList<QualitySizeUpdateResponse> Updates { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
