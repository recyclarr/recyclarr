namespace Recyclarr.Config.Parsing;

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
    public SonarrMediaNamingConfigYaml? MediaNaming { get; init; }
}
