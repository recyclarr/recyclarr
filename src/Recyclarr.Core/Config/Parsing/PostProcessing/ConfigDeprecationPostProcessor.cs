using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;

namespace Recyclarr.Config.Parsing.PostProcessing;

public class ConfigDeprecationPostProcessor(ConfigDeprecations deprecations) : IConfigPostProcessor
{
    [SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
    public RootConfigYaml Process(RootConfigYaml config)
    {
        return config with
        {
            Radarr = config.Radarr?.ToDictionary(
                x => x.Key,
                x => deprecations.CheckAndTransform(x.Value)
            ),
            Sonarr = config.Sonarr?.ToDictionary(
                x => x.Key,
                x => deprecations.CheckAndTransform(x.Value)
            ),
        };
    }
}
