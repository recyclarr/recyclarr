using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config;

public static class ConfigExtensions
{
    public static IEnumerable<IServiceConfiguration> GetConfigsOfType(
        this IEnumerable<IServiceConfiguration> configs,
        SupportedServices? serviceType)
    {
        return configs.Where(x => serviceType is null || serviceType.Value == x.ServiceType);
    }

    public static bool DoesConfigExist(this IEnumerable<IServiceConfiguration> configs, string name)
    {
        return configs.Any(x => x.InstanceName.EqualsIgnoreCase(name));
    }

    public static bool IsConfigEmpty(this RootConfigYaml config)
    {
        var sonarr = config.Sonarr?.Count ?? 0;
        var radarr = config.Radarr?.Count ?? 0;
        return sonarr + radarr == 0;
    }
}
