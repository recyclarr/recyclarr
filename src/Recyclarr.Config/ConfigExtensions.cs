using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;

namespace Recyclarr.Config;

public static class ConfigExtensions
{
    public static IEnumerable<IServiceConfiguration> GetConfigsOfType(
        this IEnumerable<IServiceConfiguration> configs,
        SupportedServices? serviceType)
    {
        return configs.Where(x => serviceType is null || serviceType.Value == x.ServiceType);
    }

    public static bool IsConfigEmpty(this RootConfigYaml? config)
    {
        var sonarr = config?.Sonarr?.Count ?? 0;
        var radarr = config?.Radarr?.Count ?? 0;
        return sonarr + radarr == 0;
    }

    public static IEnumerable<IServiceConfiguration> GetConfigsBasedOnSettings(
        this IEnumerable<IServiceConfiguration> configs,
        ConfigFilterCriteria criteria)
    {
        // later, if we filter by "operation type" (e.g. CFs, quality sizes) it's just another
        // ".Where()" in the LINQ expression below.
        return configs.GetConfigsOfType(criteria.Service)
            .Where(x => criteria.Instances.IsEmpty() ||
                criteria.Instances!.Any(y => y.EqualsIgnoreCase(x.InstanceName)));
    }

    public static IEnumerable<string> GetSplitInstances(this IEnumerable<IServiceConfiguration> configs)
    {
        return configs
            .GroupBy(x => x.BaseUrl)
            .Where(x => x.Count() > 1)
            .SelectMany(x => x.Select(y => y.InstanceName));
    }

    public static IEnumerable<string> GetInvalidInstanceNames(
        this IEnumerable<IServiceConfiguration> configs,
        ConfigFilterCriteria criteria)
    {
        if (criteria.Instances is null || criteria.Instances.Count == 0)
        {
            return Array.Empty<string>();
        }

        var configInstances = configs.Select(x => x.InstanceName).ToList();
        return criteria.Instances
            .Where(x => !configInstances.Contains(x, StringComparer.InvariantCultureIgnoreCase));
    }

    public static IEnumerable<string> GetDuplicateInstanceNames(this IEnumerable<IServiceConfiguration> configs)
    {
        return configs
            .GroupBy(x => x.InstanceName, StringComparer.InvariantCultureIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.First().InstanceName)
            .ToList();
    }
}
