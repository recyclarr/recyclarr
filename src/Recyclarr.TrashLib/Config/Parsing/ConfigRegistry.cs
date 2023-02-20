using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigRegistry : IConfigRegistry
{
    private readonly Dictionary<SupportedServices, List<IServiceConfiguration>> _configs = new();

    public void Add(IServiceConfiguration config)
    {
        _configs.GetOrCreate(config.ServiceType).Add(config);
    }

    private IEnumerable<IServiceConfiguration> GetConfigsOfType(SupportedServices? serviceType)
    {
        return _configs
            .Where(x => serviceType is null || serviceType.Value == x.Key)
            .SelectMany(x => x.Value);
    }

    public IEnumerable<IServiceConfiguration> GetConfigsBasedOnSettings(ISyncSettings settings)
    {
        // later, if we filter by "operation type" (e.g. release profiles, CFs, quality sizes) it's just another
        // ".Where()" in the LINQ expression below.
        return GetConfigsOfType(settings.Service)
            .Where(x => settings.Instances.IsEmpty() ||
                settings.Instances!.Any(y => y.EqualsIgnoreCase(x.InstanceName)));
    }

    public int Count => _configs.Count;

    public bool DoesConfigExist(string name)
    {
        return _configs.Values.Any(x => x.Any(y => y.InstanceName.EqualsIgnoreCase(name)));
    }
}
