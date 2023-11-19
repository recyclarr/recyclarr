using System.IO.Abstractions;
using AutoMapper;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Platform;
using Serilog.Context;

namespace Recyclarr.Config.Parsing;

public class ConfigurationLoader(
    ILogger log,
    ConfigParser parser,
    IMapper mapper,
    ConfigValidationExecutor validator,
    IEnumerable<IConfigPostProcessor> postProcessors)
    : IConfigurationLoader
{
    public IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(file));
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(string yaml)
    {
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(yaml));
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(Func<TextReader> streamFactory)
    {
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(streamFactory));
    }

    private IReadOnlyCollection<IServiceConfiguration> ProcessLoadedConfigs(RootConfigYaml? config)
    {
        if (config is null)
        {
            return Array.Empty<IServiceConfiguration>();
        }

        config = postProcessors.Aggregate(config, (current, processor) => processor.Process(current));

        if (config.IsConfigEmpty())
        {
            log.Warning("Configuration is empty");
        }

        if (!validator.Validate(config))
        {
            return Array.Empty<IServiceConfiguration>();
        }

        var convertedConfigs = new List<IServiceConfiguration>();
        convertedConfigs.AddRange(MapConfigs<RadarrConfigYaml, RadarrConfiguration>(config.Radarr));
        convertedConfigs.AddRange(MapConfigs<SonarrConfigYaml, SonarrConfiguration>(config.Sonarr));
        return convertedConfigs;
    }

    private IEnumerable<IServiceConfiguration> MapConfigs<TConfigYaml, TServiceConfig>(
        IReadOnlyDictionary<string, TConfigYaml>? configs)
        where TServiceConfig : ServiceConfiguration
        where TConfigYaml : ServiceConfigYaml
    {
        if (configs is null)
        {
            return Array.Empty<IServiceConfiguration>();
        }

        return configs.Select(x => mapper.Map<TServiceConfig>(x.Value) with {InstanceName = x.Key});
    }
}
