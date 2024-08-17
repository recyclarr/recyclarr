namespace Recyclarr.Compatibility.Sonarr;

public record SonarrCapabilities(Version Version)
{
    public static Version MinimumVersion { get; } = new("4.0.0.0");

    public bool QualityDefinitionLimitsIncreased => Version >= new Version(4, 0, 8, 2158);
}
