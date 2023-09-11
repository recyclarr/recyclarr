using Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;
using Serilog.Context;

namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing;

public class IncludePostProcessor : IConfigPostProcessor
{
    private readonly ILogger _log;
    private readonly ConfigParser _parser;
    private readonly ConfigValidationExecutor _validator;
    private readonly IYamlIncludeResolver _includeResolver;

    public IncludePostProcessor(
        ILogger log,
        ConfigParser parser,
        ConfigValidationExecutor validator,
        IYamlIncludeResolver includeResolver)
    {
        _log = log;
        _parser = parser;
        _validator = validator;
        _includeResolver = includeResolver;
    }

    public RootConfigYaml Process(RootConfigYaml config)
    {
        return new RootConfigYaml
        {
            Radarr = ProcessIncludes(config.Radarr, new RadarrConfigMerger(), SupportedServices.Radarr),
            Sonarr = ProcessIncludes(config.Sonarr, new SonarrConfigMerger(), SupportedServices.Sonarr)
        };
    }

    private IReadOnlyDictionary<string, T>? ProcessIncludes<T>(
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
                .Select(x => LoadYamlInclude<T>(x, serviceType))
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

    private T LoadYamlInclude<T>(IYamlInclude includeType, SupportedServices serviceType)
        where T : ServiceConfigYaml
    {
        var yamlFile = _includeResolver.GetIncludePath(includeType, serviceType);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, $"Include {yamlFile.Name}");

        var configToMerge = _parser.Load<T>(yamlFile);
        if (configToMerge is null)
        {
            throw new YamlIncludeException($"Failed to parse include file: {yamlFile.FullName}");
        }

        if (!_validator.Validate(configToMerge))
        {
            throw new YamlIncludeException($"Validation of included YAML failed: {yamlFile.FullName}");
        }

        if (configToMerge.BaseUrl is not null)
        {
            _log.Warning("`base_url` is not allowed in YAML includes");
        }

        if (configToMerge.ApiKey is not null)
        {
            _log.Warning("`api_key` is not allowed in YAML includes");
        }

        if (configToMerge.Include is not null)
        {
            _log.Warning("Nested `include` directives are not supported");
        }

        return configToMerge;
    }
}
