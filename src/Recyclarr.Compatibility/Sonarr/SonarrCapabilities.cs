namespace Recyclarr.Compatibility.Sonarr;

public record SonarrCapabilities
{
    public SonarrCapabilities()
    {
    }

    public SonarrCapabilities(Version version)
    {
        Version = version;
    }

    public static Version MinimumVersion { get; } = new("4.0.0.0");

    public Version Version { get; init; } = new();
}
