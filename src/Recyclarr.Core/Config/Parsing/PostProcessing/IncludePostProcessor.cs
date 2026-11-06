using System.IO.Abstractions;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Logging;
using Recyclarr.TrashGuide;
using Serilog.Context;

namespace Recyclarr.Config.Parsing.PostProcessing;

public sealed class IncludePostProcessor(
    ILogger log,
    ConfigParser parser,
    ConfigValidationExecutor validator,
    IYamlIncludeResolver includeResolver,
    ConfigDeprecations deprecations
) : IConfigPostProcessor
{
    public RootConfigYaml Process(RootConfigYaml config)
    {
        return new RootConfigYaml
        {
            Radarr = ProcessIncludes(
                config.Radarr,
                new RadarrConfigMerger(),
                SupportedServices.Radarr
            ),
            Sonarr = ProcessIncludes(
                config.Sonarr,
                new SonarrConfigMerger(),
                SupportedServices.Sonarr
            ),
        };
    }

    private Dictionary<string, T?>? ProcessIncludes<T>(
        IReadOnlyDictionary<string, T?>? configs,
        ServiceConfigMerger<T> merger,
        SupportedServices serviceType
    )
        where T : ServiceConfigYaml, new()
    {
        if (configs is null)
        {
            return null;
        }

        var mergedConfigs = new Dictionary<string, T?>();

        foreach (var (key, config) in configs)
        {
            if (config?.Include is null)
            {
                mergedConfigs.Add(key, config);
                continue;
            }

            try
            {
                // Combine all includes together first
                var aggregateInclude = config
                    .Include.Select(x =>
                    {
                        var yamlFile = includeResolver.GetIncludePath(x, serviceType);
                        using var logScope = LogContext.PushProperty(
                            LogProperty.Scope,
                            $"Include {yamlFile.Name}"
                        );
                        return deprecations.CheckAndTransform(LoadYamlInclude<T>(yamlFile));
                    })
                    .Aggregate(new T(), merger.Merge!);

                // Merge the config into the aggregated includes so that root config values overwrite included values.
                mergedConfigs.Add(
                    key,
                    merger.Merge(aggregateInclude, config) with
                    {
                        BaseUrl = config.BaseUrl,
                        ApiKey = config.ApiKey,

                        // No reason to keep these around anymore now that they have been merged
                        Include = null,
                    }
                );
            }
            catch (YamlIncludeException e)
            {
                throw new YamlIncludeException($"Instance '{key}': {e.Message}", e.InnerException);
            }
        }

        return mergedConfigs;
    }

    private T LoadYamlInclude<T>(IFileInfo yamlFile)
        where T : ServiceConfigYaml
    {
        T configToMerge;
        try
        {
            configToMerge =
                parser.Load<T>(yamlFile)
                ?? throw new YamlIncludeException($"Include file is empty: {yamlFile.FullName}");
        }
        catch (ConfigParsingException e)
        {
            throw new YamlIncludeException($"Failed to parse include file: {yamlFile.FullName}", e);
        }

        if (!validator.Validate(configToMerge))
        {
            throw new YamlIncludeException(
                $"Validation of included YAML failed: {yamlFile.FullName}"
            );
        }

        if (configToMerge.BaseUrl is not null)
        {
            log.Warning("`base_url` is not allowed in YAML includes");
        }

        if (configToMerge.ApiKey is not null)
        {
            log.Warning("`api_key` is not allowed in YAML includes");
        }

        if (configToMerge.Include is not null)
        {
            log.Warning("Nested `include` directives are not supported");
        }

        return configToMerge;
    }
}
