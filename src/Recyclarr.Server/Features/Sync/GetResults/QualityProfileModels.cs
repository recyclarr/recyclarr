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

/// <summary>
/// Fields both services' quality profile results share. Each service's pipelines response names
/// its concrete type, so no discriminator is needed (ADR-023).
/// </summary>
internal abstract record QualityProfilePipelineResponse
{
    public required PipelineStatus Status { get; init; }

    /// <summary>
    /// On services that store sizes per quality profile (Sonarr v5), quality size outcomes appear
    /// here with the affected profile as their subject.
    /// </summary>
    public required IReadOnlyList<OutcomeResponse> Outcomes { get; init; }

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
