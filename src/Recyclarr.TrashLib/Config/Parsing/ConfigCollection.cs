using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigCollection : IConfigCollection
{
    private readonly Dictionary<SupportedServices, List<ServiceConfiguration>> _configs = new();

    public void Add(SupportedServices configType, ServiceConfiguration config)
    {
        _configs.GetOrCreate(configType).Add(config);
    }

    public IReadOnlyCollection<T> Get<T>(SupportedServices serviceType) where T : ServiceConfiguration
    {
        return _configs[serviceType].Cast<T>().ToList();
    }

    public bool DoesConfigExist(string name)
    {
        return _configs.Values.Any(x => x.Any(y => y.InstanceName.EqualsIgnoreCase(name)));
    }
}
