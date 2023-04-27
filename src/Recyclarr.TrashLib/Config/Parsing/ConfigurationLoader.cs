using System.IO.Abstractions;
using AutoMapper;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly ConfigParser _parser;
    private readonly IMapper _mapper;

    public ConfigurationLoader(ConfigParser parser, IMapper mapper)
    {
        _parser = parser;
        _mapper = mapper;
    }

    public ICollection<IServiceConfiguration> LoadMany(
        IEnumerable<IFileInfo> configFiles,
        SupportedServices? desiredServiceType = null)
    {
        return configFiles
            .SelectMany(x => Load(x, desiredServiceType))
            .ToList();
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file, SupportedServices? desiredServiceType = null)
    {
        return ProcessLoadedConfigs(_parser.Load(file), desiredServiceType);
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(string yaml, SupportedServices? desiredServiceType = null)
    {
        return ProcessLoadedConfigs(_parser.Load(yaml), desiredServiceType);
    }

    public IReadOnlyCollection<IServiceConfiguration> Load(
        Func<TextReader> streamFactory,
        SupportedServices? desiredServiceType = null)
    {
        return ProcessLoadedConfigs(_parser.Load(streamFactory), desiredServiceType);
    }

    private IReadOnlyCollection<IServiceConfiguration> ProcessLoadedConfigs(
        RootConfigYaml? configs,
        SupportedServices? desiredServiceType)
    {
        if (configs is null)
        {
            return Array.Empty<IServiceConfiguration>();
        }

        var convertedConfigs = new List<IServiceConfiguration>();

        if (desiredServiceType is null or SupportedServices.Radarr)
        {
            convertedConfigs.AddRange(
                ValidateAndMap<RadarrConfigYaml, RadarrConfiguration>(configs.Radarr));
        }

        if (desiredServiceType is null or SupportedServices.Sonarr)
        {
            convertedConfigs.AddRange(
                ValidateAndMap<SonarrConfigYaml, SonarrConfiguration>(configs.Sonarr));
        }

        return convertedConfigs;
    }

    private IEnumerable<IServiceConfiguration> ValidateAndMap<TConfigYaml, TServiceConfig>(
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
