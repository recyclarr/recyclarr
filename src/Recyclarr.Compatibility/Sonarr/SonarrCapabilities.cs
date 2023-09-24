namespace Recyclarr.Compatibility.Sonarr;

public record SonarrCapabilities
{
    public static Version MinimumVersion { get; } = new("3.0.9.1549");

    public Version Version { get; init; } = new();

    public bool SupportsCustomFormats { get; init; }
}
