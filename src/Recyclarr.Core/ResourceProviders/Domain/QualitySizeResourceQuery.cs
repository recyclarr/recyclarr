using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class QualitySizeResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<QualitySizeResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetQualitySizes<RadarrQualitySizeResource>(serviceType),
            SupportedServices.Sonarr => GetQualitySizes<SonarrQualitySizeResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetQualitySizes<TResource>(SupportedServices serviceType)
        where TResource : QualitySizeResource
    {
        log.Debug("QualitySize: Querying {Service} quality sizes", serviceType);
        var files = registry.Get<TResource>();
        log.Debug("QualitySize: Found {Count} quality size files in registry", files.Count);

        var result = loader
            .Load<TResource>(files, GlobalJsonSerializerSettings.Guide)
            .Select(tuple => tuple.Resource)
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug(
            "QualitySize: Retrieved {Count} {Service} quality size definitions",
            result.Count,
            serviceType
        );
        return result;
    }
}
