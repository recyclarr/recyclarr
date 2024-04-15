using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Models;

public record RadarrMovieNamingConfig
{
    public bool? Rename { get; init; }
    public string? Standard { get; init; }
}

public record RadarrMediaNamingConfig
{
    public string? Folder { get; init; }
    public RadarrMovieNamingConfig? Movie { get; init; }
}

public record RadarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Radarr;

    public RadarrMediaNamingConfig MediaNaming { get; init; } = new();
}
