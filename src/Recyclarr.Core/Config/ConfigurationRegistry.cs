using System.IO.Abstractions;
using AutoMapper;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.TrashGuide;
using Serilog.Context;

namespace Recyclarr.Config;

public class ConfigurationRegistry(
    ConfigurationLoader loader,
    IConfigurationFinder finder,
    IFileSystem fs,
    ConfigValidationExecutor validator,
    IMapper mapper
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
        var loadedConfigs = configs
            .SelectMany(loader.Load)
            .Where(filterCriteria.InstanceMatchesCriteria)
            .ToList();

        var dupeInstances = loadedConfigs.GetDuplicateInstanceNames().ToList();
        if (dupeInstances.Count != 0)
        {
            throw new DuplicateInstancesException(dupeInstances);
        }

        var invalidInstances = loadedConfigs.GetNonExistentInstanceNames(filterCriteria).ToList();
        if (invalidInstances.Count != 0)
        {
            throw new InvalidInstancesException(invalidInstances);
        }

        var splitInstances = loadedConfigs.GetSplitInstances().ToList();
        if (splitInstances.Count != 0)
        {
            throw new SplitInstancesException(splitInstances);
        }

        List<IServiceConfiguration> validatedConfigs = [];

        foreach (var config in loadedConfigs)
        {
            using var logScope = LogContext.PushProperty(LogProperty.Scope, config.Filename);

            if (!validator.Validate(config.Yaml, YamlValidatorRuleSets.RootConfig))
            {
                continue;
            }

            validatedConfigs.Add(
                config.Yaml switch
                {
                    RadarrConfigYaml => MapConfig<RadarrConfiguration>(config),
                    SonarrConfigYaml => MapConfig<SonarrConfiguration>(config),
                    _ => throw new InvalidOperationException("Unknown config type"),
                }
            );
        }

        return validatedConfigs;
    }

    private IServiceConfiguration MapConfig<TServiceConfig>(LoadedConfigYaml config)
        where TServiceConfig : ServiceConfiguration
    {
        return mapper.Map<TServiceConfig>(config.Yaml) with { InstanceName = config.InstanceName };
    }
}
