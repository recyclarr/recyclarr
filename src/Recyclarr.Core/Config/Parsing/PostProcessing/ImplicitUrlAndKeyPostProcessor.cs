using Recyclarr.Config.Secrets;

namespace Recyclarr.Config.Parsing.PostProcessing;

public class ImplicitUrlAndKeyPostProcessor(ILogger log, ISecretsProvider secrets)
    : IConfigPostProcessor
{
    public RootConfigYaml Process(RootConfigYaml config)
    {
        return new RootConfigYaml
        {
            Radarr = ProcessService(config.Radarr),
            Sonarr = ProcessService(config.Sonarr),
        };
    }

    private Dictionary<string, T?>? ProcessService<T>(IReadOnlyDictionary<string, T?>? services)
        where T : ServiceConfigYaml
    {
        return services?.ToDictionary(x => x.Key, x => FillUrlAndKey(x.Key, x.Value));
    }

    private T? FillUrlAndKey<T>(string instanceName, T? config)
        where T : ServiceConfigYaml
    {
        if (config is null)
        {
            return null;
        }

        return config with
        {
            ApiKey = config.ApiKey ?? GetSecret(instanceName, "api_key"),
            BaseUrl = config.BaseUrl ?? GetSecret(instanceName, "base_url"),
        };
    }

    private string? GetSecret(string instanceName, string property)
    {
        log.Debug(
            "Obtain {Property} implicitly for instance {InstanceName}",
            property,
            instanceName
        );
        return secrets.Secrets.GetValueOrDefault($"{instanceName}_{property}");
    }
}
