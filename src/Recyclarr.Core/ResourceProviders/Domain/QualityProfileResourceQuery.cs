using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class QualityProfileResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<RadarrQualityProfileResource> GetRadarr()
    {
        var result = GetQualityProfiles<RadarrQualityProfileResource>();
        log.Debug("QualityProfile: Retrieved {Count} Radarr quality profiles", result.Count);
        return result;
    }

    public IReadOnlyList<SonarrQualityProfileResource> GetSonarr()
    {
        var result = GetQualityProfiles<SonarrQualityProfileResource>();
        log.Debug("QualityProfile: Retrieved {Count} Sonarr quality profiles", result.Count);
        return result;
    }

    private List<TResource> GetQualityProfiles<TResource>()
        where TResource : QualityProfileResource
    {
        var files = registry.Get<TResource>();
        log.Debug("QualityProfile: Found {Count} quality profile files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Metadata);

        return loaded
            .Select(tuple => tuple.Resource)
            .GroupBy(g => g.TrashId)
            .Select(g => g.Last())
            .ToList();
    }
}
