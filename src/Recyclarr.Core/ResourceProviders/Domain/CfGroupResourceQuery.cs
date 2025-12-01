using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class CfGroupResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<RadarrCfGroupResource> GetRadarr()
    {
        var result = GetCfGroups<RadarrCfGroupResource>();
        log.Debug("CfGroup: Retrieved {Count} Radarr CF groups", result.Count);
        return result;
    }

    public IReadOnlyList<SonarrCfGroupResource> GetSonarr()
    {
        var result = GetCfGroups<SonarrCfGroupResource>();
        log.Debug("CfGroup: Retrieved {Count} Sonarr CF groups", result.Count);
        return result;
    }

    private List<TResource> GetCfGroups<TResource>()
        where TResource : CfGroupResource
    {
        var files = registry.Get<TResource>();
        log.Debug("CfGroup: Found {Count} CF group files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Metadata);

        return loaded
            .Select(tuple => tuple.Resource)
            .GroupBy(g => g.TrashId)
            .Select(g => g.Last())
            .ToList();
    }
}
