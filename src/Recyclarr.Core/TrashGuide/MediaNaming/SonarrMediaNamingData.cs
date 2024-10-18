namespace Recyclarr.TrashGuide.MediaNaming;

public record SonarrEpisodeNamingData
{
    public IReadOnlyDictionary<string, string> Standard { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Daily { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Anime { get; init; } = new Dictionary<string, string>();
}

public record SonarrMediaNamingData
{
    public IReadOnlyDictionary<string, string> Season { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Series { get; init; } = new Dictionary<string, string>();
    public SonarrEpisodeNamingData Episodes { get; init; } = new();
}
