using System.IO.Abstractions;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;

namespace Recyclarr.Config;

public class ConfigurationRegistry(IConfigurationLoader loader, IConfigurationFinder finder, IFileSystem fs)
    : IConfigurationRegistry
{
    public IReadOnlyCollection<IServiceConfiguration> FindAndLoadConfigs(ConfigFilterCriteria? filterCriteria = null)
    {
        filterCriteria ??= new ConfigFilterCriteria();

        var manualConfigs = filterCriteria.ManualConfigFiles;
        var configs = manualConfigs is not null && manualConfigs.Count != 0
            ? PrepareManualConfigs(manualConfigs)
            : finder.GetConfigFiles();

        return LoadAndFilterConfigs(configs, filterCriteria).ToList();
    }

    private List<IFileInfo> PrepareManualConfigs(IEnumerable<string> manualConfigs)
    {
        var configFiles = manualConfigs
            .Select(x => fs.FileInfo.New(x))
            .ToLookup(x => x.Exists);

        if (configFiles[false].Any())
        {
            throw new InvalidConfigurationFilesException(configFiles[false].ToList());
        }

        return configFiles[true].ToList();
    }

    private IEnumerable<IServiceConfiguration> LoadAndFilterConfigs(
        IEnumerable<IFileInfo> configs,
        ConfigFilterCriteria filterCriteria)
    {
        var loadedConfigs = configs.SelectMany(x => loader.Load(x)).ToList();

        var dupeInstances = loadedConfigs.GetDuplicateInstanceNames().ToList();
        if (dupeInstances.Count != 0)
        {
            throw new DuplicateInstancesException(dupeInstances);
        }

        var invalidInstances = loadedConfigs.GetInvalidInstanceNames(filterCriteria).ToList();
        if (invalidInstances.Count != 0)
        {
            throw new InvalidInstancesException(invalidInstances);
        }

        var splitInstances = loadedConfigs.GetSplitInstances().ToList();
        if (splitInstances.Count != 0)
        {
            throw new SplitInstancesException(splitInstances);
        }

        return loadedConfigs.GetConfigsBasedOnSettings(filterCriteria);
    }
}
