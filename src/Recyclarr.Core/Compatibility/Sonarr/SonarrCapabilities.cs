namespace Recyclarr.Compatibility.Sonarr;

public record SonarrCapabilities(Version Version)
{
    public static Version MinimumVersion { get; } = new("4.0.0.0");

    public bool QualityDefinitionLimitsIncreased => Version >= new Version(4, 0, 8, 2158);

    // Sonarr v5 moved size limits from global quality definitions onto quality profile items.
    public bool QualitySizesPerProfile => Version.Major >= 5;

    // Sonarr v5 still serves the deprecated v3 API, but Recyclarr drives each major through the
    // API generation it was built for.
    public ServiceApiTarget ApiTarget =>
        Version.Major >= 5 ? ServiceApiTarget.SonarrV5 : ServiceApiTarget.SonarrV3;
}
