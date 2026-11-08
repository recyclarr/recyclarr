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
    public required PlannedValueResponse<int?> Score { get; init; }
}

/// <summary>
/// One configured profile and every value Recyclarr manages on it, changed or not. Values neither
/// the configuration nor the guide sets keep their service value (or the service default for a
/// created profile).
/// </summary>
internal record QualityProfileItemResponse
{
    public required ResourceAction Action { get; init; }
    public required QualityProfileIdentityResponse Identity { get; init; }
    public required PlannedValueResponse<string> Name { get; init; }
    public required PlannedValueResponse<bool?> UpgradeAllowed { get; init; }
    public required PlannedValueResponse<string?> UpgradeUntilQuality { get; init; }
    public required PlannedValueResponse<int?> UpgradeUntilScore { get; init; }
    public required PlannedValueResponse<int?> MinimumFormatScore { get; init; }
    public required PlannedValueResponse<int?> MinimumUpgradeFormatScore { get; init; }

    public required PlannedValueResponse<
        IReadOnlyList<QualityProfileLayoutResponse>
    > Qualities { get; init; }

    /// <summary>Every custom format score in the profile.</summary>
    public required IReadOnlyList<QualityProfileScoreResponse> CustomFormatScores { get; init; }
}

internal sealed record RadarrQualityProfileItemResponse : QualityProfileItemResponse
{
    public required PlannedValueResponse<string?> Language { get; init; }
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
