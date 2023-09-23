using Recyclarr.Common;

namespace Recyclarr.Config;

public record ConfigFilterCriteria
{
    public IReadOnlyCollection<string>? ManualConfigFiles { get; init; }
    public SupportedServices? Service { get; init; }
    public IReadOnlyCollection<string>? Instances { get; init; }
}
