using Recyclarr.Common;

namespace Recyclarr.Config.Models;

public record SonarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Sonarr;

    public IList<ReleaseProfileConfig> ReleaseProfiles { get; init; } =
        Array.Empty<ReleaseProfileConfig>();

    public SonarrMediaNamingConfig MediaNaming { get; init; } = new();
}

public class ReleaseProfileConfig
{
    public IReadOnlyCollection<string> TrashIds { get; init; } = Array.Empty<string>();
    public bool StrictNegativeScores { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public SonarrProfileFilterConfig? Filter { get; init; }
}

public class SonarrProfileFilterConfig
{
    public IReadOnlyCollection<string> Include { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Exclude { get; init; } = Array.Empty<string>();
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
