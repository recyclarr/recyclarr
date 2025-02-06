using Recyclarr.Config.Parsing;

namespace Recyclarr.Config;

public static class ConfigExtensions
{
    public static bool IsConfigEmpty(this RootConfigYaml? config)
    {
        var sonarr = config?.Sonarr?.Count ?? 0;
        var radarr = config?.Radarr?.Count ?? 0;
        return sonarr + radarr == 0;
    }

    // public static ICollection<string> GetSplitInstances(this IEnumerable<LoadedConfigYaml> configs)
    // {
    //     return configs
    //         .GroupBy(x => x.Yaml.BaseUrl)
    //         .Where(x => x.Count() > 1)
    //         .SelectMany(x => x.Select(y => y.InstanceName))
    //         .ToList();
    // }

    // public static IEnumerable<string> GetNonExistentInstanceNames(
    //     this IEnumerable<LoadedConfigYaml> configs,
    //     ConfigFilterCriteria criteria
    // )
    // {
    //     if (criteria.Instances is not { Count: > 0 })
    //     {
    //         return [];
    //     }
    //
    //     var names = configs.Select(x => x.InstanceName).ToList();
    //
    //     return criteria.Instances.Where(x =>
    //         !names.Contains(x, StringComparer.InvariantCultureIgnoreCase)
    //     );
    // }

    // public static IEnumerable<string> GetDuplicateInstanceNames(
    //     this IReadOnlyCollection<LoadedConfigYaml> configs
    // )
    // {
    //     return configs
    //         .Select(x => x.InstanceName)
    //         .GroupBy(x => x, StringComparer.InvariantCultureIgnoreCase)
    //         .Where(x => x.Count() > 1)
    //         .Select(x => x.First())
    //         .ToList();
    // }
}
