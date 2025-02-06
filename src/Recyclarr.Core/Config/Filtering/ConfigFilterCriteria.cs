using Recyclarr.Config.Parsing;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Filtering;

public record ConfigFilterCriteria
{
    public IReadOnlyCollection<string> ManualConfigFiles { get; init; } = [];
    public SupportedServices? Service { get; init; }
    public IReadOnlyCollection<string> Instances { get; init; } = [];

    public bool InstanceMatchesCriteria(LoadedConfigYaml loadedConfig)
    {
        return (Service is null || Service == loadedConfig.ServiceType)
            && (
                Instances.Count == 0
                || Instances.Contains(loadedConfig.InstanceName, StringComparer.OrdinalIgnoreCase)
            );
    }
}
