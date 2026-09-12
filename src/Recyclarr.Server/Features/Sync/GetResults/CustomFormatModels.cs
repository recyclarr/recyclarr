namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum CustomFormatSource
{
    FlatConfig,
    ProfileFormatItems,
    CfGroupImplicit,
    CfGroupExplicit,
}

internal enum CustomFormatInclusionReason
{
    None,
    Required,
    Default,
    Selected,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatSelectionResponse(
    CustomFormatSource Source,
    string? GroupName,
    CustomFormatInclusionReason InclusionReason,
    IReadOnlyList<string> ProfileNames
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatCreateResponse(
    TrashIdNameResponse Identity,
    CustomFormatSelectionResponse SelectionProvenance
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatUpdateResponse(
    TrashIdNameResponse Identity,
    CustomFormatSelectionResponse SelectionProvenance
)
{
    public ValueChangeResponse<string>? Name { get; init; }
    public ValueChangeResponse<bool>? IncludeWhenRenaming { get; init; }
    public IReadOnlyList<string> SpecificationsAdded { get; init; } = [];
    public IReadOnlyList<string> SpecificationsChanged { get; init; } = [];
    public IReadOnlyList<string> SpecificationsRemoved { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatStateConflictResponse(
    TrashIdNameResponse Identity,
    TrashIdNameResponse ManagedIdentity,
    int ServiceId
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatAmbiguousMatchResponse(
    TrashIdNameResponse Identity,
    IReadOnlyList<NamedServiceResourceResponse> ServiceMatches
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatOutcomesResponse
{
    public IReadOnlyList<string> ReferenceMismatches { get; init; } = [];
    public IReadOnlyList<string> GroupReferenceMismatches { get; init; } = [];
    public IReadOnlyList<TrashIdNameResponse> IncompatibleGroups { get; init; } = [];
    public IReadOnlyList<TrashIdNameResponse> EmptyGroups { get; init; } = [];
    public IReadOnlyList<NamedIdentityResponse> Adopted { get; init; } = [];
    public IReadOnlyList<CustomFormatAmbiguousMatchResponse> AmbiguousMatches { get; init; } = [];
    public IReadOnlyList<CustomFormatStateConflictResponse> StateConflicts { get; init; } = [];
    public IReadOnlyList<TrashIdNameResponse> CreateRejected { get; init; } = [];
    public IReadOnlyList<TrashIdNameResponse> UpdateRejected { get; init; } = [];
    public IReadOnlyList<TrashIdNameResponse> DeleteRejected { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record NamedIdentityResponse(TrashIdNameResponse Identity, int ServiceId);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatPipelineResponse(
    PipelineStatus Status,
    CustomFormatOutcomesResponse Outcomes,
    IReadOnlyList<CustomFormatCreateResponse> Creates,
    IReadOnlyList<CustomFormatUpdateResponse> Updates,
    IReadOnlyList<TrashIdNameResponse> Deletes
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}
