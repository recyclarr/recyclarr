using System.IO.Abstractions;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Logging;
using Recyclarr.TrashGuide;
using Serilog.Context;

namespace Recyclarr.Config.Parsing;

public record LoadedConfigYaml(
    string InstanceName,
    SupportedServices ServiceType,
    ServiceConfigYaml Yaml
)
{
    public IFileInfo? YamlPath { get; init; }
}

public class ConfigurationLoader(
    ILogger log,
    ConfigParser parser,
    IOrderedEnumerable<IConfigPostProcessor> postProcessors
)
{
    public IReadOnlyCollection<LoadedConfigYaml> Load(IFileInfo file)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(file))
            .Select(x => x with { YamlPath = file })
            .ToList();
    }

    public IReadOnlyCollection<LoadedConfigYaml> Load(string yaml)
    {
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(yaml));
    }

    public IReadOnlyCollection<LoadedConfigYaml> Load(Func<TextReader> streamFactory)
    {
        return ProcessLoadedConfigs(parser.Load<RootConfigYaml>(streamFactory));
    }

    private List<LoadedConfigYaml> ProcessLoadedConfigs(RootConfigYaml? config)
    {
        if (config is null)
        {
            return [];
        }

        config = postProcessors.Aggregate(
            config,
            (current, processor) => processor.Process(current)
        );

        if (config.IsConfigEmpty())
        {
            log.Warning("Configuration is empty");
        }

        return Enumerable
            .Empty<LoadedConfigYaml>()
            .Concat(AsLoadedConfig(config.Radarr, SupportedServices.Radarr))
            .Concat(AsLoadedConfig(config.Sonarr, SupportedServices.Sonarr))
            .ToList();

        IEnumerable<LoadedConfigYaml> AsLoadedConfig<T>(
            IReadOnlyDictionary<string, T?>? configs,
            SupportedServices serviceType
        )
            where T : ServiceConfigYaml
        {
            return configs
                    ?.Where(x => x.Value is not null)
                    .Select(kvp => new LoadedConfigYaml(kvp.Key, serviceType, kvp.Value!)) ?? [];
        }
    }
}
