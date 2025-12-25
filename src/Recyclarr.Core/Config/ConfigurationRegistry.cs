using System.IO.Abstractions;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using Serilog.Context;

namespace Recyclarr.Config;

public class ConfigurationRegistry(
    ConfigurationLoader loader,
    IConfigurationFinder finder,
    IFileSystem fs,
    ConfigFilterProcessor filterProcessor
)
{
    public IReadOnlyCollection<IServiceConfiguration> FindAndLoadConfigs(
        ConfigFilterCriteria? filterCriteria = null
    )
    {
        filterCriteria ??= new ConfigFilterCriteria();

        var manualConfigs = filterCriteria.ManualConfigFiles;
        var configs =
            manualConfigs.Count != 0
                ? PrepareManualConfigs(manualConfigs)
                : finder.GetConfigFiles();

        return LoadAndFilterConfigs(configs, filterCriteria).ToList();
    }

    private List<IFileInfo> PrepareManualConfigs(IEnumerable<string> manualConfigs)
    {
        var configFiles = manualConfigs.Select(x => fs.FileInfo.New(x)).ToLookup(x => x.Exists);

        if (configFiles[false].Any())
        {
            throw new InvalidConfigurationFilesException(configFiles[false].ToList());
        }

        return configFiles[true].ToList();
    }

    private List<IServiceConfiguration> LoadAndFilterConfigs(
        IEnumerable<IFileInfo> configs,
        ConfigFilterCriteria filterCriteria
    )
    {
        var allLoadedConfigs = configs.SelectMany(loader.Load).ToList();

        // Extract all instance names before filtering for better error messages
        var allInstanceNames = allLoadedConfigs
            .Select(x => x.InstanceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var loadedConfigs = allLoadedConfigs.Where(filterCriteria.InstanceMatchesCriteria).ToList();

        var filteredConfigs = filterProcessor.FilterAndRender(
            filterCriteria,
            loadedConfigs,
            allInstanceNames
        );

        return filteredConfigs
            .Select(x =>
            {
                using var logScope = LogContext.PushProperty(LogProperty.Scope, x.YamlPath);
                return x.Yaml switch
                {
                    RadarrConfigYaml radarr => radarr.ToRadarrConfiguration(
                        x.InstanceName,
                        x.YamlPath
                    ),
                    SonarrConfigYaml sonarr => sonarr.ToSonarrConfiguration(
                        x.InstanceName,
                        x.YamlPath
                    ),
                    _ => throw new InvalidOperationException("Unknown config type"),
                };
            })
            .ToList();
    }
}
