using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum QualityProfileIdentityKind
{
    GuideBacked,
    UserDefined,
}

internal enum QualityProfileLayoutKind
{
    Quality,
    Group,
}

internal enum QualityProfileScoreReason
{
    Set,
    Reset,
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileIdentityResponse(QualityProfileIdentityKind Kind, string Name)
{
    public string? TrashId { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileLayoutResponse(
    QualityProfileLayoutKind Kind,
    string Name,
    bool Allowed
)
{
    public IReadOnlyList<string>? Qualities { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileScoreResponse(string Name, int Score)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? TrashId { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileControlledStateResponse
{
    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required bool? UpgradeAllowed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? UpgradeUntilQuality { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int? UpgradeUntilScore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int? MinimumFormatScore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int? MinimumUpgradeFormatScore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? Language { get; init; }

    public required IReadOnlyList<QualityProfileLayoutResponse> Qualities { get; init; }
    public required IReadOnlyList<QualityProfileScoreResponse> CustomFormatScores { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileCreateResponse(
    QualityProfileIdentityResponse Identity,
    QualityProfileControlledStateResponse State
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileScoreChangeResponse(
    string Name,
    ValueChangeResponse<int> Value,
    QualityProfileScoreReason Reason
)
{
    public string? TrashId { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileUpdateResponse(QualityProfileIdentityResponse Identity)
{
    public ValueChangeResponse<string>? Name { get; init; }
    public ValueChangeResponse<bool?>? UpgradeAllowed { get; init; }
    public ValueChangeResponse<string?>? UpgradeUntilQuality { get; init; }
    public ValueChangeResponse<int?>? UpgradeUntilScore { get; init; }
    public ValueChangeResponse<int?>? MinimumFormatScore { get; init; }
    public ValueChangeResponse<int?>? MinimumUpgradeFormatScore { get; init; }
    public ValueChangeResponse<string?>? Language { get; init; }
    public ValueChangeResponse<
        IReadOnlyList<QualityProfileLayoutResponse>
    >? QualityLayout { get; init; }
    public IReadOnlyList<QualityProfileScoreChangeResponse> CustomFormatScores { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileIdentityOutcomeResponse(
    QualityProfileIdentityResponse Identity
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileNamedOutcomeResponse(string Name);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileAdoptedResponse(
    QualityProfileIdentityResponse Identity,
    int ServiceId
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileMinimumScoreResponse(
    QualityProfileIdentityResponse Identity,
    int MinimumScore,
    int TotalPositiveScore,
    int MaximumScore
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileQualityNameResponse(
    QualityProfileIdentityResponse Identity,
    string QualityName
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileNamesResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<string> Names
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileResetReferencesResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Patterns
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileRenameBlockedResponse(
    QualityProfileIdentityResponse Identity,
    NamedServiceResourceResponse Conflict
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileAmbiguousMatchResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<NamedServiceResourceResponse> ServiceMatches
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileFormatReferenceResponse(string Name, string TrashId);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileScoreCollisionResponse(
    QualityProfileFormatReferenceResponse Existing,
    QualityProfileFormatReferenceResponse Rejected,
    int ServiceId
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileOutcomesResponse
{
    public IReadOnlyList<string> ReferenceMismatches { get; init; } = [];
    public IReadOnlyList<QualityProfileNamedOutcomeResponse> DuplicateNames { get; init; } = [];
    public IReadOnlyList<QualityProfileScoreCollisionResponse> ScoreCollisions { get; init; } = [];
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> NotFound { get; init; } = [];
    public IReadOnlyList<QualityProfileAdoptedResponse> Adopted { get; init; } = [];
    public IReadOnlyList<QualityProfileMinimumScoreResponse> MinimumScoresUnsatisfied { get; init; } =
    [];
    public IReadOnlyList<QualityProfileQualityNameResponse> InvalidCutoffs { get; init; } = [];
    public IReadOnlyList<QualityProfileQualityNameResponse> UnavailableCutoffs { get; init; } = [];
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> QualitiesRequired { get; init; } =
    [];
    public IReadOnlyList<QualityProfileNamesResponse> QualityReferenceMismatches { get; init; } =
    [];
    public IReadOnlyList<QualityProfileResetReferencesResponse> ResetScoreReferenceMismatches { get; init; } =
    [];
    public IReadOnlyList<QualityProfileRenameBlockedResponse> RenameBlocked { get; init; } = [];
    public IReadOnlyList<QualityProfileAmbiguousMatchResponse> AmbiguousMatches { get; init; } = [];
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> CreateRejected { get; init; } = [];
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> UpdateRejected { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfilePipelineResponse(
    PipelineStatus Status,
    QualityProfileOutcomesResponse Outcomes,
    IReadOnlyList<QualityProfileCreateResponse> Creates,
    IReadOnlyList<QualityProfileUpdateResponse> Updates
)
{
    public BlockingPipeline? BlockedBy { get; init; }
}
