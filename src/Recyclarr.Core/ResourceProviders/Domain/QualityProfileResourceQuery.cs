using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class QualityProfileResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<QualityProfileResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetQualityProfiles<RadarrQualityProfileResource>(
                serviceType
            ),
            SupportedServices.Sonarr => GetQualityProfiles<SonarrQualityProfileResource>(
                serviceType
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetQualityProfiles<TResource>(SupportedServices serviceType)
        where TResource : QualityProfileResource
    {
        log.Debug("QualityProfile: Querying {Service} quality profiles", serviceType);
        var files = registry.Get<TResource>();
        log.Debug("QualityProfile: Found {Count} quality profile files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Guide);

        var result = loaded
            .Select(tuple => tuple.Resource)
            .GroupBy(g => g.TrashId)
            .Select(g => g.Last())
            .ToList();

        log.Debug(
            "QualityProfile: Retrieved {Count} {Service} quality profiles",
            result.Count,
            serviceType
        );
        return result;
    }
}
