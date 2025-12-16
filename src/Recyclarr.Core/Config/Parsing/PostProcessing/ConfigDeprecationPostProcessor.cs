using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Logging;
using Recyclarr.Sync.Events;
using Serilog.Context;

namespace Recyclarr.Config.Parsing.PostProcessing;

public class ConfigDeprecationPostProcessor(
    ConfigDeprecations deprecations,
    ISyncScopeFactory scopeFactory
) : IConfigPostProcessor
{
    [SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
    public RootConfigYaml Process(RootConfigYaml config)
    {
        return config with
        {
            Radarr = config.Radarr?.ToDictionary(
                x => x.Key,
                x => CheckWithInstanceContext(x.Key, x.Value)
            ),
            Sonarr = config.Sonarr?.ToDictionary(
                x => x.Key,
                x => CheckWithInstanceContext(x.Key, x.Value)
            ),
        };
    }

    private T? CheckWithInstanceContext<T>(string instanceName, T? yaml)
        where T : ServiceConfigYaml
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, instanceName);
        using var instanceScope = scopeFactory.SetInstance(instanceName);
        return deprecations.CheckAndTransform(yaml);
    }
}
