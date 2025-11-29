using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class QualitySizeResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<RadarrQualitySizeResource> GetRadarr()
    {
        log.Debug("QualitySize: Querying Radarr quality sizes");
        var result = GetQualitySizes<RadarrQualitySizeResource>();
        log.Debug("QualitySize: Retrieved {Count} Radarr quality size definitions", result.Count);
        return result;
    }

    public IReadOnlyList<SonarrQualitySizeResource> GetSonarr()
    {
        log.Debug("QualitySize: Querying Sonarr quality sizes");
        var result = GetQualitySizes<SonarrQualitySizeResource>();
        log.Debug("QualitySize: Retrieved {Count} Sonarr quality size definitions", result.Count);
        return result;
    }

    private List<TResource> GetQualitySizes<TResource>()
        where TResource : QualitySizeResource
    {
        var files = registry.Get<TResource>();
        log.Debug("QualitySize: Found {Count} quality size files in registry", files.Count);

        return loader
            .Load<TResource>(files)
            .Select(tuple => tuple.Resource)
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
    }
}
