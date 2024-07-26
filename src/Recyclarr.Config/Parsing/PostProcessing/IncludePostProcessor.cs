using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Platform;
using Recyclarr.TrashGuide;
using Serilog.Context;

namespace Recyclarr.Config.Parsing.PostProcessing;

public sealed class IncludePostProcessor(
    ILogger log,
    ConfigParser parser,
    ConfigValidationExecutor validator,
    IYamlIncludeResolver includeResolver,
    ConfigDeprecations deprecations)
    : IConfigPostProcessor, IDisposable
{
    private IDisposable? _logScope;

    public void Dispose()
    {
        _logScope?.Dispose();
    }

    public RootConfigYaml Process(RootConfigYaml config)
    {
        try
        {
            config = config with
            {
                Radarr = ProcessIncludes(config.Radarr, new RadarrConfigMerger(), SupportedServices.Radarr),
                Sonarr = ProcessIncludes(config.Sonarr, new SonarrConfigMerger(), SupportedServices.Sonarr)
            };
        }
        finally
        {
            _logScope?.Dispose();
        }

        return config;
    }

    private Dictionary<string, T>? ProcessIncludes<T>(
        IReadOnlyDictionary<string, T>? configs,
        ServiceConfigMerger<T> merger,
        SupportedServices serviceType)
        where T : ServiceConfigYaml, new()
    {
        if (configs is null)
        {
            return null;
        }

        var mergedConfigs = new Dictionary<string, T>();

        foreach (var (key, config) in configs)
        {
            if (config.Include is null)
            {
                mergedConfigs.Add(key, config);
                continue;
            }

            // Combine all includes together first
            var aggregateInclude = config.Include
                .Select(x =>
                {
                    var include = LoadYamlInclude<T>(x, serviceType);
                    return deprecations.CheckAndTransform(include);
                })
                .Aggregate(new T(), merger.Merge);

            // Merge the config into the aggregated includes so that root config values overwrite included values.
            mergedConfigs.Add(key, merger.Merge(aggregateInclude, config) with
            {
                BaseUrl = config.BaseUrl,
                ApiKey = config.ApiKey,

                // No reason to keep these around anymore now that they have been merged
                Include = null
            });
        }

        return mergedConfigs;
    }

    private T LoadYamlInclude<T>(IYamlInclude includeDirective, SupportedServices serviceType)
        where T : ServiceConfigYaml
    {
        var yamlFile = includeResolver.GetIncludePath(includeDirective, serviceType);
        _logScope = LogContext.PushProperty(LogProperty.Scope, $"Include {yamlFile.Name}");

        var configToMerge = parser.Load<T>(yamlFile);
        if (configToMerge is null)
        {
            throw new YamlIncludeException($"Failed to parse include file: {yamlFile.FullName}");
        }

        if (!validator.Validate(configToMerge))
        {
            throw new YamlIncludeException($"Validation of included YAML failed: {yamlFile.FullName}");
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
