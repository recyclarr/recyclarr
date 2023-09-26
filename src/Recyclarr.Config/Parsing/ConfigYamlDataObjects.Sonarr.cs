using JetBrains.Annotations;

namespace Recyclarr.Config.Parsing;

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

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record SonarrEpisodeNamingConfigYaml
{
    public bool? Rename { get; init; }
    public string? Standard { get; init; }
    public string? Daily { get; init; }
    public string? Anime { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record SonarrMediaNamingConfigYaml
{
    public string? Season { get; init; }
    public string? Series { get; init; }
    public SonarrEpisodeNamingConfigYaml? Episodes { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record SonarrConfigYaml : ServiceConfigYaml
{
    public IReadOnlyCollection<ReleaseProfileConfigYaml>? ReleaseProfiles { get; init; }
    public SonarrMediaNamingConfigYaml? MediaNaming { get; init; }
}
