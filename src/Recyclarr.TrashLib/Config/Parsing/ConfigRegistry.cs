using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigRegistry : IConfigRegistry
{
    private readonly Dictionary<SupportedServices, List<ServiceConfiguration>> _configs = new();

    public void Add(SupportedServices configType, ServiceConfiguration config)
    {
        _configs.GetOrCreate(configType).Add(config);
    }

    public IReadOnlyCollection<T> GetConfigsOfType<T>(SupportedServices serviceType) where T : ServiceConfiguration
    {
        if (_configs.TryGetValue(serviceType, out var configs))
        {
            return configs.Cast<T>().ToList();
        }

        return Array.Empty<T>();
    }

    public bool DoesConfigExist(string name)
    {
        return _configs.Values.Any(x => x.Any(y => y.InstanceName.EqualsIgnoreCase(name)));
    }
}
