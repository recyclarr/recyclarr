using TrashLib.Config.Services;

namespace TrashLib.Services.Sonarr.Config;

public class SonarrConfiguration : ServiceConfiguration
{
    public IList<ReleaseProfileConfig> ReleaseProfiles { get; init; } = Array.Empty<ReleaseProfileConfig>();
    public string QualityDefinition { get; init; } = "";
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
