using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Processors;

public static class ProcessorExtensions
{
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

    public static IEnumerable<string> GetSplitInstances(this IEnumerable<IServiceConfiguration> configs)
    {
        return configs
            .GroupBy(x => x.BaseUrl)
            .Where(x => x.Count() > 1)
            .SelectMany(x => x.Select(y => y.InstanceName));
    }

    public static IEnumerable<string> GetInvalidInstanceNames(
        this ISyncSettings settings,
        IEnumerable<IServiceConfiguration> configs)
    {
        if (settings.Instances is null)
        {
            return Array.Empty<string>();
        }

        var configInstances = configs.Select(x => x.InstanceName).ToList();
        return settings.Instances
            .Where(x => !configInstances.Contains(x, StringComparer.InvariantCultureIgnoreCase));
    }
}
