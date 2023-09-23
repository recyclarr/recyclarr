using System.IO.Abstractions;
using AutoMapper;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Platform;
using Serilog.Context;

namespace Recyclarr.Config.Parsing;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger _log;
    private readonly ConfigParser _parser;
    private readonly IMapper _mapper;
    private readonly ConfigValidationExecutor _validator;
    private readonly IEnumerable<IConfigPostProcessor> _postProcessors;

    public ConfigurationLoader(
        ILogger log,
        ConfigParser parser,
        IMapper mapper,
        ConfigValidationExecutor validator,
        IEnumerable<IConfigPostProcessor> postProcessors)
    {
        _log = log;
        _parser = parser;
        _mapper = mapper;
        _validator = validator;
        _postProcessors = postProcessors;
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);
        return ProcessLoadedConfigs(_parser.Load<RootConfigYaml>(file));
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(string yaml)
    {
        return ProcessLoadedConfigs(_parser.Load<RootConfigYaml>(yaml));
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(Func<TextReader> streamFactory)
    {
        return ProcessLoadedConfigs(_parser.Load<RootConfigYaml>(streamFactory));
    }

    private IReadOnlyCollection<IServiceConfiguration> ProcessLoadedConfigs(RootConfigYaml? config)
    {
        if (config is null)
        {
            return Array.Empty<IServiceConfiguration>();
        }

        config = _postProcessors.Aggregate(config, (current, processor) => processor.Process(current));

        if (config.IsConfigEmpty())
        {
            _log.Warning("Configuration is empty");
        }

        if (!_validator.Validate(config))
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

        return configs.Select(x => _mapper.Map<TServiceConfig>(x.Value) with {InstanceName = x.Key});
    }
}
