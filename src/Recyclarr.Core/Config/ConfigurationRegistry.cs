using System.IO.Abstractions;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Logging;
using Serilog.Context;

namespace Recyclarr.Config;

public class ConfigurationRegistry(
    ConfigurationLoader loader,
    IConfigurationFinder finder,
    IFileSystem fs,
    ConfigFilterProcessor filterProcessor,
    IConfigDiagnosticCollector diagnosticCollector
)
{
    public ConfigRegistryResult FindAndLoadConfigs(ConfigFilterCriteria? filterCriteria = null)
    {
        filterCriteria ??= new ConfigFilterCriteria();

        var manualConfigs = filterCriteria.ManualConfigFiles;
        var configFiles =
            manualConfigs.Count != 0
                ? PrepareManualConfigs(manualConfigs)
                : finder.GetConfigFiles();

        return LoadAndFilterConfigs(configFiles, filterCriteria);
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

    private ConfigRegistryResult LoadAndFilterConfigs(
        IEnumerable<IFileInfo> configFiles,
        ConfigFilterCriteria filterCriteria
    )
    {
        var allLoadedConfigs = new List<LoadedConfigYaml>();
        var failures = new List<ConfigParsingException>();

        foreach (var file in configFiles)
        {
            try
            {
                allLoadedConfigs.AddRange(loader.Load(file));
            }
            catch (ConfigParsingException e)
            {
                e.FilePath = file;
                failures.Add(e);
            }
            catch (YamlIncludeException e) when (e.InnerException is ConfigParsingException inner)
            {
                inner.FilePath = file;
                failures.Add(inner);
            }
        }

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

        var configs = filteredConfigs
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

        return new ConfigRegistryResult(configs, failures, diagnosticCollector.Deprecations);
    }
}
