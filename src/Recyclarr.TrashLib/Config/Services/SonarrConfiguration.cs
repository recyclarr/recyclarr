using JetBrains.Annotations;

namespace Recyclarr.TrashLib.Config.Services;

public record SonarrConfiguration : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Sonarr;

    public IList<ReleaseProfileConfig> ReleaseProfiles { get; [UsedImplicitly] init; } =
        Array.Empty<ReleaseProfileConfig>();
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
