using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.Services.Sonarr.Config;

public class SonarrConfiguration : ServiceConfiguration
{
    public IList<ReleaseProfileConfig> ReleaseProfiles { get; [UsedImplicitly] init; } =
        Array.Empty<ReleaseProfileConfig>();

    public string QualityDefinition { get; [UsedImplicitly] init; } = "";
}

public class ReleaseProfileConfig
{
    public IReadOnlyCollection<string> TrashIds { get; [UsedImplicitly] init; } = Array.Empty<string>();
    public bool StrictNegativeScores { get; [UsedImplicitly] init; }
    public IReadOnlyCollection<string> Tags { get; [UsedImplicitly] init; } = Array.Empty<string>();
    public SonarrProfileFilterConfig? Filter { get; [UsedImplicitly] init; }
}

public class SonarrProfileFilterConfig
{
    public IReadOnlyCollection<string> Include { get; [UsedImplicitly] init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Exclude { get; [UsedImplicitly] init; } = Array.Empty<string>();
}
