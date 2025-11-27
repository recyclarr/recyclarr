using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class QualitySizeResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader
)
{
    public IReadOnlyList<RadarrQualitySizeResource> GetRadarr()
    {
        return GetQualitySizes<RadarrQualitySizeResource>();
    }

    public IReadOnlyList<SonarrQualitySizeResource> GetSonarr()
    {
        return GetQualitySizes<SonarrQualitySizeResource>();
    }

    private List<TResource> GetQualitySizes<TResource>()
        where TResource : QualitySizeResource
    {
        var files = registry.Get<TResource>();
        return loader
            .Load<TResource>(files)
            .Select(tuple => tuple.Resource)
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
    }
}
