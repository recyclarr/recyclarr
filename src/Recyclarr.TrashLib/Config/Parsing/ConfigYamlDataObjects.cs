using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

public record QualityScoreConfigYaml
{
    public string? Name { get; [UsedImplicitly] init; }
    public int? Score { get; [UsedImplicitly] init; }
}

public record CustomFormatConfigYaml
{
    public IReadOnlyCollection<string>? TrashIds { get; [UsedImplicitly] init; }
    public IReadOnlyCollection<QualityScoreConfigYaml>? QualityProfiles { get; [UsedImplicitly] init; }
}

public record QualitySizeConfigYaml
{
    public string? Type { get; [UsedImplicitly] init; }
    public decimal? PreferredRatio { get; [UsedImplicitly] init; }
}

public record QualityProfileConfigYaml
{
    public string? Name { get; [UsedImplicitly] init; }
    public bool ResetUnmatchedScores { get; [UsedImplicitly] init; }
}

public record ServiceConfigYaml
{
    public string? ApiKey { get; [UsedImplicitly] init; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
    public string? BaseUrl { get; [UsedImplicitly] init; }

    public bool DeleteOldCustomFormats { get; [UsedImplicitly] init; }
    public bool ReplaceExistingCustomFormats { get; [UsedImplicitly] init; }

    public IReadOnlyCollection<CustomFormatConfigYaml>? CustomFormats { get; [UsedImplicitly] init; }
    public QualitySizeConfigYaml? QualityDefinition { get; [UsedImplicitly] init; }
    public IReadOnlyCollection<QualityProfileConfigYaml>? QualityProfiles { get; [UsedImplicitly] init; }
}

public record ReleaseProfileFilterConfigYaml
{
    public IReadOnlyCollection<string>? Include { get; [UsedImplicitly] init; }
    public IReadOnlyCollection<string>? Exclude { get; [UsedImplicitly] init; }
}

public record ReleaseProfileConfigYaml
{
    public IReadOnlyCollection<string>? TrashIds { get; [UsedImplicitly] init; }
    public bool StrictNegativeScores { get; [UsedImplicitly] init; }
    public IReadOnlyCollection<string>? Tags { get; [UsedImplicitly] init; }
    public ReleaseProfileFilterConfigYaml? Filter { get; [UsedImplicitly] init; }
}

// This is usually empty (or the same as ServiceConfigYaml) on purpose.
// If empty, it is kept around to make it clear that this one is dedicated to Radarr.
public record RadarrConfigYaml : ServiceConfigYaml;

public record SonarrConfigYaml : ServiceConfigYaml
{
    public IReadOnlyCollection<ReleaseProfileConfigYaml>? ReleaseProfiles { get; [UsedImplicitly] init; }
}

public record RootConfigYaml
{
    public IReadOnlyDictionary<string, RadarrConfigYaml>? Radarr { get; [UsedImplicitly] init; }
    public IReadOnlyDictionary<string, SonarrConfigYaml>? Sonarr { get; [UsedImplicitly] init; }

    // This exists for validation purposes only.
    [YamlIgnore]
    public IEnumerable<RadarrConfigYaml> RadarrValues
        => Radarr?.Select(x => x.Value) ?? Array.Empty<RadarrConfigYaml>();

    // This exists for validation purposes only.
    [YamlIgnore]
    public IEnumerable<SonarrConfigYaml> SonarrValues
        => Sonarr?.Select(x => x.Value) ?? Array.Empty<SonarrConfigYaml>();
}
