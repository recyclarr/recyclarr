namespace Recyclarr.ResourceProviders.Domain;

public record SonarrEpisodeNamingResource
{
    public IReadOnlyDictionary<string, string> Standard { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Daily { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Anime { get; init; } =
        new Dictionary<string, string>();
}

public record SonarrMediaNamingResource
{
    public IReadOnlyDictionary<string, string> Season { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Series { get; init; } =
        new Dictionary<string, string>();
    public SonarrEpisodeNamingResource Episodes { get; init; } = new();
}
