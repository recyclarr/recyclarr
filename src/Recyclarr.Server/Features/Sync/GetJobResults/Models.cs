using System.Text.Json.Serialization;

namespace Recyclarr.Server.Features.Sync.GetJobResults;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResultsRequest
{
    public Guid Id { get; init; }
}

// Where a custom format came from, so a consumer can group changes by origin. Mirrors CfSource
// plus the names that give the origin meaning: the group it belongs to and the profiles that
// pulled it in.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatSourceResponse(string Source)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupName { get; init; }

    public IReadOnlyList<string> ProfileNames { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatChangeResponse(string Action, string Name, string TrashId)
{
    // Absent when the custom format has no recorded source, which consumers treat the same as
    // CfSource.FlatConfig.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CustomFormatSourceResponse? Source { get; init; }

    // Only meaningful for custom formats sourced from a group; absent otherwise.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InclusionReason { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CustomFormatsResultResponse
{
    public required IReadOnlyList<CustomFormatChangeResponse> Changes { get; init; }
    public required int UnchangedCount { get; init; }
}

// One quality or quality group. Groups carry nested Items; plain qualities leave it empty.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityItemResponse(string Name, bool Allowed)
{
    public IReadOnlyList<QualityItemResponse> Items { get; init; } = [];
}

// The profile-level fields a consumer renders side by side as current vs. desired.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileFieldsResponse
{
    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UpgradeAllowed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinFormatScore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinUpgradeFormatScore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UpgradeUntilQuality { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UpgradeUntilScore { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileQualitiesResponse(
    string SortMode,
    IReadOnlyList<QualityItemResponse> Current,
    IReadOnlyList<QualityItemResponse> Desired
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record FormatScoreChangeResponse(
    string Name,
    int CurrentScore,
    int NewScore,
    string Reason
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfileChangeResponse(string Name, string ChangeReason)
{
    public required QualityProfileFieldsResponse Current { get; init; }
    public required QualityProfileFieldsResponse Desired { get; init; }

    // Absent when the profile has no quality overrides configured, in which case there is no
    // current-vs-desired quality list worth rendering.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QualityProfileQualitiesResponse? Qualities { get; init; }

    public required IReadOnlyList<FormatScoreChangeResponse> ScoreChanges { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualityProfilesResultResponse
{
    public required IReadOnlyList<QualityProfileChangeResponse> Profiles { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizeItemResponse(
    string Quality,
    decimal Min,
    decimal Max,
    decimal Preferred
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record QualitySizesResultResponse
{
    // Only sizes that differ from what the service currently has.
    public required IReadOnlyList<QualitySizeItemResponse> Items { get; init; }

    // A Max or Preferred at or above its limit means "unlimited" to the service.
    public required decimal MaxLimit { get; init; }
    public required decimal PreferredLimit { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SonarrNamingResultResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RenameEpisodes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeriesFolderFormat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeasonFolderFormat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StandardEpisodeFormat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DailyEpisodeFormat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnimeEpisodeFormat { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record RadarrNamingResultResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RenameMovies { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StandardMovieFormat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MovieFolderFormat { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record MediaManagementResultResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PropersAndRepacks { get; init; }
}

// Per ADR-009/ADR-014: a null operation means it produced nothing for this instance (skipped, not
// applicable to the service type, or a dependency failed) and is omitted from the wire entirely.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncInstanceResultResponse(string Instance)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CustomFormatsResultResponse? CustomFormats { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QualityProfilesResultResponse? QualityProfiles { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QualitySizesResultResponse? QualitySizes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SonarrNamingResultResponse? SonarrNaming { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RadarrNamingResultResponse? RadarrNaming { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MediaManagementResultResponse? MediaManagement { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GetSyncJobResultsResponse
{
    public required Guid Id { get; init; }

    // Only instances whose results have been captured. Empty while the job is still running.
    public required IReadOnlyList<SyncInstanceResultResponse> Instances { get; init; }
}
