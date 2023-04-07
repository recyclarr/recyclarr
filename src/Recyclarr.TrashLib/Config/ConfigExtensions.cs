using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Config;

public static class ConfigExtensions
{
    public static IEnumerable<IServiceConfiguration> GetConfigsOfType(
        this IEnumerable<IServiceConfiguration> configs,
        SupportedServices? serviceType)
    {
        return configs.Where(x => serviceType is null || serviceType.Value == x.ServiceType);
    }

    public static IEnumerable<IServiceConfiguration> GetConfigsBasedOnSettings(
        this IEnumerable<IServiceConfiguration> configs,
        ISyncSettings settings)
    {
        // later, if we filter by "operation type" (e.g. release profiles, CFs, quality sizes) it's just another
        // ".Where()" in the LINQ expression below.
        return configs.GetConfigsOfType(settings.Service)
            .Where(x => settings.Instances.IsEmpty() ||
                settings.Instances!.Any(y => y.EqualsIgnoreCase(x.InstanceName)));
    }

    public static bool DoesConfigExist(this IEnumerable<IServiceConfiguration> configs, string name)
    {
        return configs.Any(x => x.InstanceName.EqualsIgnoreCase(name));
    }

    public static bool IsConfigEmpty(this RootConfigYamlLatest config)
    {
        var sonarr = config.Sonarr?.Count ?? 0;
        var radarr = config.Radarr?.Count ?? 0;
        return sonarr + radarr == 0;
    }
}
