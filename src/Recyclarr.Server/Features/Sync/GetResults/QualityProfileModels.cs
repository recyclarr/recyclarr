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

/// <summary>Why a profile field has its desired value.</summary>
internal enum QualityProfileFieldReason
{
    [Description(
        "Recyclarr manages the value: the configuration or guide sets it, or Recyclarr picks it "
            + "because the service requires one (for example, the first allowed quality as the "
            + "upgrade-until quality)."
    )]
    Set,

    [Description(
        "Nothing sets the value, so it keeps its value in the service (the service default for a "
            + "created profile)."
    )]
    Unmanaged,
}

/// <summary>
/// A profile field and why it has its desired value. The reason sits beside the value, never in
/// it.
/// </summary>
internal sealed record QualityProfileFieldResponse<T>
{
    public required QualityProfileFieldReason Reason { get; init; }
    public required PlannedValueResponse<T> Value { get; init; }
}

/// <summary>Why a custom format score has its desired value.</summary>
internal enum QualityProfileScoreReason
{
    [Description("The configuration or guide sets the score.")]
    Set,

    [Description(
        "`reset_unmatched_scores` sets the score to 0 because the configuration does not score "
            + "the custom format."
    )]
    Reset,

    [Description("Nothing sets the score, so it keeps its value in the service.")]
    Unmanaged,
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

    /// <summary>Null when the custom format is not in the configuration.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? TrashId { get; init; }

    public required QualityProfileScoreReason Reason { get; init; }
    public required PlannedValueResponse<int?> Value { get; init; }
}

/// <summary>
/// One configured profile and every value on it, changed or not. Each field's `reason` tells
/// whether Recyclarr manages it or it keeps its service value.
/// </summary>
internal record QualityProfileItemResponse
{
    public required ResourceAction Action { get; init; }
    public required QualityProfileIdentityResponse Identity { get; init; }
    public required PlannedValueResponse<string> Name { get; init; }
    public required QualityProfileFieldResponse<bool?> UpgradeAllowed { get; init; }
    public required QualityProfileFieldResponse<string?> UpgradeUntilQuality { get; init; }
    public required QualityProfileFieldResponse<int?> UpgradeUntilScore { get; init; }
    public required QualityProfileFieldResponse<int?> MinimumFormatScore { get; init; }
    public required QualityProfileFieldResponse<int?> MinimumUpgradeFormatScore { get; init; }

    public required QualityProfileFieldResponse<
        IReadOnlyList<QualityProfileLayoutResponse>
    > Qualities { get; init; }

    /// <summary>Every custom format score in the profile.</summary>
    public required IReadOnlyList<QualityProfileScoreResponse> CustomFormatScores { get; init; }
}

internal sealed record RadarrQualityProfileItemResponse : QualityProfileItemResponse
{
    public required QualityProfileFieldResponse<string?> Language { get; init; }
}

internal sealed record SonarrQualityProfileItemResponse : QualityProfileItemResponse
{
    /// <summary>
    /// Sizes of every quality in the profile that the configuration sizes. Only Sonarr v5 stores
    /// sizes per quality profile; empty on earlier versions.
    /// </summary>
    public required IReadOnlyList<QualitySizeItemResponse> QualitySizes { get; init; }
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
    /// <summary>Every valid configured profile. A profile skipped by an outcome is absent.</summary>
    public required IReadOnlyList<SonarrQualityProfileItemResponse> Items { get; init; }
}

internal sealed record RadarrQualityProfilePipelineResponse : QualityProfilePipelineResponse
{
    /// <summary>Every valid configured profile. A profile skipped by an outcome is absent.</summary>
    public required IReadOnlyList<RadarrQualityProfileItemResponse> Items { get; init; }
}
