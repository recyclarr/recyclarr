using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetResults;

/// <summary>Where a quality profile's definition comes from.</summary>
internal enum QualityProfileIdentityKind
{
    [Description("A guide quality profile; `trashId` is present.")]
    GuideBacked,

    [Description("A profile defined only in the configuration.")]
    UserDefined,
}

internal enum QualityProfileLayoutKind
{
    Quality,
    Group,
}

/// <summary>Why a custom format score changed.</summary>
internal enum QualityProfileScoreReason
{
    [Description("The configuration or guide sets the score.")]
    Set,

    [Description(
        "`reset_unmatched_scores` sets the score to 0 because the configuration does not score "
            + "the custom format."
    )]
    Reset,
}

internal sealed record QualityProfileIdentityResponse
{
    public required QualityProfileIdentityKind Kind { get; init; }
    public required string Name { get; init; }
    public string? TrashId { get; init; }
}

/// <summary>A quality or quality group in a profile, in profile order.</summary>
internal sealed record QualityProfileLayoutResponse
{
    public required QualityProfileLayoutKind Kind { get; init; }
    public required string Name { get; init; }
    public required bool Allowed { get; init; }

    /// <summary>The member qualities; present only for a group.</summary>
    public IReadOnlyList<string>? Qualities { get; init; }
}

internal sealed record QualityProfileScoreResponse
{
    public required string Name { get; init; }
    public required int Score { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? TrashId { get; init; }
}

/// <summary>
/// The profile fields Recyclarr sets when it creates a profile. A null field means neither the
/// configuration nor the guide gives a value.
/// </summary>
internal record QualityProfileControlledStateResponse
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

    public required IReadOnlyList<QualityProfileLayoutResponse> Qualities { get; init; }
    public required IReadOnlyList<QualityProfileScoreResponse> CustomFormatScores { get; init; }
}

internal sealed record RadarrQualityProfileControlledStateResponse
    : QualityProfileControlledStateResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? Language { get; init; }
}

internal sealed record SonarrQualityProfileControlledStateResponse
    : QualityProfileControlledStateResponse
{
    /// <summary>
    /// Sizes assigned to the profile's qualities. Only Sonarr v5 stores sizes per quality profile;
    /// empty on earlier versions.
    /// </summary>
    public required IReadOnlyList<QualitySizeResponse> QualitySizes { get; init; }
}

internal sealed record SonarrQualityProfileCreateResponse(
    QualityProfileIdentityResponse Identity,
    SonarrQualityProfileControlledStateResponse State
);

internal sealed record RadarrQualityProfileCreateResponse(
    QualityProfileIdentityResponse Identity,
    RadarrQualityProfileControlledStateResponse State
);

internal sealed record QualityProfileScoreChangeResponse
{
    public required string Name { get; init; }
    public required ValueChangeResponse<int> Value { get; init; }
    public required QualityProfileScoreReason Reason { get; init; }
    public string? TrashId { get; init; }
}

internal record QualityProfileUpdateResponse
{
    public required QualityProfileIdentityResponse Identity { get; init; }
    public ValueChangeResponse<string>? Name { get; init; }
    public ValueChangeResponse<bool?>? UpgradeAllowed { get; init; }
    public ValueChangeResponse<string?>? UpgradeUntilQuality { get; init; }
    public ValueChangeResponse<int?>? UpgradeUntilScore { get; init; }
    public ValueChangeResponse<int?>? MinimumFormatScore { get; init; }
    public ValueChangeResponse<int?>? MinimumUpgradeFormatScore { get; init; }
    public ValueChangeResponse<
        IReadOnlyList<QualityProfileLayoutResponse>
    >? QualityLayout { get; init; }
    public IReadOnlyList<QualityProfileScoreChangeResponse> CustomFormatScores { get; init; } = [];
}

internal sealed record RadarrQualityProfileUpdateResponse : QualityProfileUpdateResponse
{
    public ValueChangeResponse<string?>? Language { get; init; }
}

internal sealed record SonarrQualityProfileUpdateResponse : QualityProfileUpdateResponse
{
    /// <summary>
    /// Per-quality size changes. Only Sonarr v5 stores sizes per quality profile; empty on earlier
    /// versions.
    /// </summary>
    public IReadOnlyList<QualitySizeUpdateResponse> QualitySizes { get; init; } = [];
}

internal sealed record QualityProfileQualitySizeOutcomesResponse(
    QualityProfileIdentityResponse Identity,
    QualitySizeOutcomesResponse Outcomes
);

internal sealed record QualityProfileIdentityOutcomeResponse(
    QualityProfileIdentityResponse Identity
);

internal sealed record QualityProfileNamedOutcomeResponse(string Name);

internal sealed record QualityProfileAdoptedResponse(
    QualityProfileIdentityResponse Identity,
    int ServiceId
);

internal sealed record QualityProfileMinimumScoreResponse(
    QualityProfileIdentityResponse Identity,
    int MinimumScore,
    int TotalPositiveScore,
    int MaximumScore
);

internal sealed record QualityProfileQualityNameResponse(
    QualityProfileIdentityResponse Identity,
    string QualityName
);

internal sealed record QualityProfileNamesResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<string> Names
);

internal sealed record QualityProfileResetReferencesResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Patterns
);

internal sealed record QualityProfileRenameBlockedResponse(
    QualityProfileIdentityResponse Identity,
    NamedServiceResourceResponse Conflict
);

internal sealed record QualityProfileAmbiguousMatchResponse(
    QualityProfileIdentityResponse Identity,
    IReadOnlyList<NamedServiceResourceResponse> ServiceMatches
);

internal sealed record QualityProfileFormatReferenceResponse(string Name, string TrashId);

internal sealed record QualityProfileScoreCollisionResponse(
    QualityProfileFormatReferenceResponse Existing,
    QualityProfileFormatReferenceResponse Rejected,
    int ServiceId
);

internal sealed record QualityProfileOutcomesResponse
{
    /// <summary>Configured trash IDs that match no guide quality profile.</summary>
    public IReadOnlyList<string> ReferenceMismatches { get; init; } = [];

    /// <summary>Profile names configured more than once; none of them sync.</summary>
    public IReadOnlyList<QualityProfileNamedOutcomeResponse> DuplicateNames { get; init; } = [];

    /// <summary>
    /// Two custom formats in one profile that resolve to the same service custom format. The
    /// `existing` score is kept and the `rejected` score is dropped.
    /// </summary>
    public IReadOnlyList<QualityProfileScoreCollisionResponse> ScoreCollisions { get; init; } = [];

    /// <summary>
    /// Profiles that only receive scores (Recyclarr does not create them) and do not exist in the
    /// service.
    /// </summary>
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> NotFound { get; init; } = [];

    /// <summary>
    /// Guide profiles that already existed in the service with the same name. Recyclarr now
    /// manages them.
    /// </summary>
    public IReadOnlyList<QualityProfileAdoptedResponse> Adopted { get; init; } = [];

    /// <summary>
    /// Profiles whose minimum custom format score is higher than both the sum of positive scores
    /// and the highest score, so no release could qualify.
    /// </summary>
    public IReadOnlyList<QualityProfileMinimumScoreResponse> MinimumScoresUnsatisfied { get; init; } =
    [];

    /// <summary>Profiles whose upgrade-until quality is a name the service does not have.</summary>
    public IReadOnlyList<QualityProfileQualityNameResponse> InvalidCutoffs { get; init; } = [];

    /// <summary>
    /// Profiles whose upgrade-until quality is not an allowed quality or group in the profile.
    /// </summary>
    public IReadOnlyList<QualityProfileQualityNameResponse> UnavailableCutoffs { get; init; } = [];

    /// <summary>New profiles that have no qualities; the service requires at least one.</summary>
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> QualitiesRequired { get; init; } =
    [];

    /// <summary>Configured quality names that the service does not have.</summary>
    public IReadOnlyList<QualityProfileNamesResponse> QualityReferenceMismatches { get; init; } =
    [];

    /// <summary>
    /// `reset_unmatched_scores.except` names that are not in the profile, and except patterns
    /// that match no custom format in the profile.
    /// </summary>
    public IReadOnlyList<QualityProfileResetReferencesResponse> ResetScoreReferenceMismatches { get; init; } =
    [];

    /// <summary>
    /// Profiles that could not be renamed because another service profile has the new name.
    /// </summary>
    public IReadOnlyList<QualityProfileRenameBlockedResponse> RenameBlocked { get; init; } = [];

    /// <summary>
    /// Profiles whose name matches more than one service profile; they are not synced.
    /// </summary>
    public IReadOnlyList<QualityProfileAmbiguousMatchResponse> AmbiguousMatches { get; init; } = [];

    /// <summary>Profiles the service refused to create.</summary>
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> CreateRejected { get; init; } = [];

    /// <summary>Profiles the service refused to update.</summary>
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> UpdateRejected { get; init; } = [];

    /// <summary>
    /// Problems with the quality sizes resolved for a profile, on services that store sizes per
    /// quality profile (Sonarr v5). Affected qualities keep their service sizes; the rest of the
    /// profile still syncs.
    /// </summary>
    public IReadOnlyList<QualityProfileQualitySizeOutcomesResponse> QualitySizeOutcomes { get; init; } =
    [];

    /// <summary>
    /// Profiles whose `quality_definition` was ignored because the service stores quality sizes
    /// globally.
    /// </summary>
    public IReadOnlyList<QualityProfileIdentityOutcomeResponse> QualityDefinitionsIgnored { get; init; } =
    [];

    /// <summary>
    /// True when the instance `quality_definition` applied to nothing: the service stores sizes
    /// per quality profile and no profile is listed under `quality_profiles`.
    /// </summary>
    public bool QualityDefinitionUnused { get; init; }
}

/// <summary>
/// Fields both services' quality profile results share. Each service's pipelines response names
/// its concrete type, so no discriminator is needed (ADR-023).
/// </summary>
internal abstract record QualityProfilePipelineResponse
{
    public required PipelineStatus Status { get; init; }
    public required QualityProfileOutcomesResponse Outcomes { get; init; }

    /// <summary>Present only when the status is `blocked`.</summary>
    public BlockingPipeline? BlockedBy { get; init; }
}

internal sealed record SonarrQualityProfilePipelineResponse : QualityProfilePipelineResponse
{
    public required IReadOnlyList<SonarrQualityProfileCreateResponse> Creates { get; init; }
    public required IReadOnlyList<SonarrQualityProfileUpdateResponse> Updates { get; init; }
}

internal sealed record RadarrQualityProfilePipelineResponse : QualityProfilePipelineResponse
{
    public required IReadOnlyList<RadarrQualityProfileCreateResponse> Creates { get; init; }
    public required IReadOnlyList<RadarrQualityProfileUpdateResponse> Updates { get; init; }
}
