using Recyclarr.Config.Parsing;

namespace Recyclarr.Config;

public static class ConfigExtensions
{
    public static bool IsConfigEmpty(this RootConfigYaml? config)
    {
        var sonarr = config?.Sonarr?.Count ?? 0;
        var radarr = config?.Radarr?.Count ?? 0;
        return sonarr + radarr == 0;
    }
}
