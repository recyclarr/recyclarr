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

internal sealed record QualitySizeTypeResponse(string Type);

internal sealed record QualitySizeGuideReferenceResponse(string Label, string TrashId);

internal sealed record QualitySizeGuideLabelsResponse(IReadOnlyList<string> Labels);

internal sealed record QualitySizeReferenceResponse(string Quality, string Type);

internal sealed record QualitySizeMinimumComparisonResponse(
    string Quality,
    QualitySizeValueResponse Minimum,
    QualitySizeValueResponse Preferred
);

internal sealed record QualitySizeMaximumComparisonResponse(
    string Quality,
    QualitySizeValueResponse Preferred,
    QualitySizeValueResponse Maximum
);

internal sealed record QualitySizeOutcomesResponse
{
    /// <summary>A configured `quality_definition.type` that the guide does not have.</summary>
    public IReadOnlyList<QualitySizeTypeResponse> DefinitionReferenceMismatches { get; init; } = [];

    /// <summary>
    /// Configured quality overrides that are not in the guide's quality size set.
    /// </summary>
    public IReadOnlyList<QualitySizeReferenceResponse> ReferenceMismatches { get; init; } = [];

    /// <summary>Guide qualities that the service does not have.</summary>
    public IReadOnlyList<string> ServiceQualitiesNotFound { get; init; } = [];

    /// <summary>
    /// A configured preferred ratio outside 0 to 1: `current` is the configured value and
    /// `desired` the value used.
    /// </summary>
    public IReadOnlyList<ValueChangeResponse<decimal>> PreferredRatiosClamped { get; init; } = [];

    /// <summary>
    /// Qualities not synced because the minimum is larger than the preferred size.
    /// </summary>
    public IReadOnlyList<QualitySizeMinimumComparisonResponse> MinimumGreaterThanPreferred { get; init; } =
    [];

    /// <summary>
    /// Qualities not synced because the preferred size is unlimited but the maximum is not.
    /// </summary>
    public IReadOnlyList<QualitySizeMaximumComparisonResponse> UnlimitedPreferredGreaterThanMaximum { get; init; } =
    [];

    /// <summary>
    /// Qualities not synced because the preferred size is larger than the maximum.
    /// </summary>
    public IReadOnlyList<QualitySizeMaximumComparisonResponse> PreferredGreaterThanMaximum { get; init; } =
    [];

    /// <summary>
    /// A guide quality profile links a quality size set (by trash ID) that the guide does not
    /// have. Reported only for services that store sizes per quality profile.
    /// </summary>
    public IReadOnlyList<QualitySizeGuideReferenceResponse> GuideReferenceMismatches { get; init; } =
    [];

    /// <summary>
    /// A guide quality profile links more than one quality size set, so none is applied. Reported
    /// only for services that store sizes per quality profile.
    /// </summary>
    public IReadOnlyList<QualitySizeGuideLabelsResponse> AmbiguousGuideReferences { get; init; } =
    [];
}

internal sealed record QualitySizePipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required QualitySizeOutcomesResponse Outcomes { get; init; }
    public required IReadOnlyList<QualitySizeUpdateResponse> Updates { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}
