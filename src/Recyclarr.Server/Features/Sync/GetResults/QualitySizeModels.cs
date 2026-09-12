namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum QualitySizeKind
{
    Numeric,
    Unlimited,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeValueResponse(QualitySizeKind Kind)
{
    public decimal? Value { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeUpdateResponse(string Quality)
{
    public ValueChangeResponse<QualitySizeValueResponse>? Minimum { get; init; }
    public ValueChangeResponse<QualitySizeValueResponse>? Preferred { get; init; }
    public ValueChangeResponse<QualitySizeValueResponse>? Maximum { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeTypeResponse(string Type);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeReferenceResponse(string Quality, string Type);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeMinimumComparisonResponse(
    string Quality,
    QualitySizeValueResponse Minimum,
    QualitySizeValueResponse Preferred
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeMaximumComparisonResponse(
    string Quality,
    QualitySizeValueResponse Preferred,
    QualitySizeValueResponse Maximum
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeOutcomesResponse
{
    public IReadOnlyList<QualitySizeTypeResponse> DefinitionReferenceMismatches { get; init; } = [];
    public IReadOnlyList<QualitySizeReferenceResponse> ReferenceMismatches { get; init; } = [];
    public IReadOnlyList<string> ServiceQualitiesNotFound { get; init; } = [];
    public IReadOnlyList<ValueChangeResponse<decimal>> PreferredRatiosClamped { get; init; } = [];
    public IReadOnlyList<QualitySizeMinimumComparisonResponse> MinimumGreaterThanPreferred { get; init; } =
    [];
    public IReadOnlyList<QualitySizeMaximumComparisonResponse> UnlimitedPreferredGreaterThanMaximum { get; init; } =
    [];
    public IReadOnlyList<QualitySizeMaximumComparisonResponse> PreferredGreaterThanMaximum { get; init; } =
    [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizePipelineResponse(
    PipelineStatus Status,
    QualitySizeOutcomesResponse Outcomes,
    IReadOnlyList<QualitySizeUpdateResponse> Updates
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}
