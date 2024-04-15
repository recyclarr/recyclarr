using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Models;

public record SonarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Sonarr;

    public SonarrMediaNamingConfig MediaNaming { get; init; } = new();
}

public record SonarrMediaNamingConfig
{
    public string? Season { get; init; }
    public string? Series { get; init; }
    public SonarrEpisodeNamingConfig? Episodes { get; init; }
}

public record SonarrEpisodeNamingConfig
{
    public bool? Rename { get; init; }
    public string? Standard { get; init; }
    public string? Daily { get; init; }
    public string? Anime { get; init; }
}
