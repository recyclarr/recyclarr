using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class CfGroupResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<CfGroupResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetCfGroups<RadarrCfGroupResource>(serviceType),
            SupportedServices.Sonarr => GetCfGroups<SonarrCfGroupResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetCfGroups<TResource>(SupportedServices serviceType)
        where TResource : CfGroupResource
    {
        log.Debug("CfGroup: Querying {Service} CF groups", serviceType);
        var files = registry.Get<TResource>();
        log.Debug("CfGroup: Found {Count} CF group files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Metadata);

        var result = loaded
            .Select(tuple => tuple.Resource)
            .GroupBy(g => g.TrashId)
            .Select(g => g.Last())
            .ToList();

        log.Debug("CfGroup: Retrieved {Count} {Service} CF groups", result.Count, serviceType);
        return result;
    }
}
