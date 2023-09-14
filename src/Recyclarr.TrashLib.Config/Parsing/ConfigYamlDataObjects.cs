using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityScoreConfigYaml
{
    public string? Name { get; init; }
    public int? Score { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record CustomFormatConfigYaml
{
    public IReadOnlyCollection<string>? TrashIds { get; init; }
    public IReadOnlyCollection<QualityScoreConfigYaml>? QualityProfiles { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualitySizeConfigYaml
{
    public string? Type { get; init; }
    public decimal? PreferredRatio { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileFormatUpgradeYaml
{
    public bool? Allowed { get; init; }
    public int? UntilScore { get; init; }
    public string? UntilQuality { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileQualityConfigYaml
{
    public string? Name { get; init; }
    public bool? Enabled { get; init; }
    public IReadOnlyCollection<string>? Qualities { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ResetUnmatchedScoresConfigYaml
{
    public bool? Enabled { get; init; }
    public IReadOnlyCollection<string>? Except { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileConfigYaml
{
    public string? Name { get; init; }
    public ResetUnmatchedScoresConfigYaml? ResetUnmatchedScores { get; init; }
    public QualityProfileFormatUpgradeYaml? Upgrade { get; init; }
    public int? MinFormatScore { get; init; }
    public QualitySortAlgorithm? QualitySort { get; init; }
    public IReadOnlyCollection<QualityProfileQualityConfigYaml>? Qualities { get; init; }
    public string? ScoreSet { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ServiceConfigYaml
{
    public string? ApiKey { get; init; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? BaseUrl { get; init; }

    public bool? DeleteOldCustomFormats { get; init; }
    public bool? ReplaceExistingCustomFormats { get; init; }

    public IReadOnlyCollection<CustomFormatConfigYaml>? CustomFormats { get; init; }
    public QualitySizeConfigYaml? QualityDefinition { get; init; }
    public IReadOnlyCollection<QualityProfileConfigYaml>? QualityProfiles { get; init; }
    public IReadOnlyCollection<IYamlInclude>? Include { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ReleaseProfileFilterConfigYaml
{
    public IReadOnlyCollection<string>? Include { get; init; }
    public IReadOnlyCollection<string>? Exclude { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ReleaseProfileConfigYaml
{
    public IReadOnlyCollection<string>? TrashIds { get; init; }
    public bool StrictNegativeScores { get; init; }
    public IReadOnlyCollection<string>? Tags { get; init; }
    public ReleaseProfileFilterConfigYaml? Filter { get; init; }
}

// This is usually empty (or the same as ServiceConfigYaml) on purpose.
// If empty, it is kept around to make it clear that this one is dedicated to Radarr.
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public record RadarrConfigYaml : ServiceConfigYaml;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record SonarrConfigYaml : ServiceConfigYaml
{
    public IReadOnlyCollection<ReleaseProfileConfigYaml>? ReleaseProfiles { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record RootConfigYaml
{
    public IReadOnlyDictionary<string, RadarrConfigYaml>? Radarr { get; init; }
    public IReadOnlyDictionary<string, SonarrConfigYaml>? Sonarr { get; init; }

    // This exists for validation purposes only.
    [YamlIgnore]
    public IEnumerable<RadarrConfigYaml> RadarrValues
        => Radarr?.Select(x => x.Value) ?? Array.Empty<RadarrConfigYaml>();

    // This exists for validation purposes only.
    [YamlIgnore]
    public IEnumerable<SonarrConfigYaml> SonarrValues
        => Sonarr?.Select(x => x.Value) ?? Array.Empty<SonarrConfigYaml>();
}
