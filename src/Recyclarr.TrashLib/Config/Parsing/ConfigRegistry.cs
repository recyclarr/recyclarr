using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigRegistry : IConfigRegistry
{
    private readonly Dictionary<SupportedServices, List<IServiceConfiguration>> _configs = new();

    public void Add(IServiceConfiguration config)
    {
        _configs.GetOrCreate(config.ServiceType).Add(config);
    }

    public IReadOnlyCollection<IServiceConfiguration> GetConfigsOfType(SupportedServices? serviceType)
    {
        return _configs
            .Where(x => serviceType is null || serviceType.Value == x.Key)
            .SelectMany(x => x.Value)
            .ToList();
    }

    public bool DoesConfigExist(string name)
    {
        return _configs.Values.Any(x => x.Any(y => y.InstanceName.EqualsIgnoreCase(name)));
    }
}
