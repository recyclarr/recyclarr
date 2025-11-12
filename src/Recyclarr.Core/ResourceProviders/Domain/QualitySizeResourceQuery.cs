using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class QualitySizeResourceQuery(IResourcePathRegistry registry, JsonResourceLoader loader)
{
    public IReadOnlyCollection<RadarrQualitySizeResource> GetRadarr()
    {
        return GetQualitySizes<RadarrQualitySizeResource>();
    }

    public IReadOnlyCollection<SonarrQualitySizeResource> GetSonarr()
    {
        return GetQualitySizes<SonarrQualitySizeResource>();
    }

    private IReadOnlyCollection<TResource> GetQualitySizes<TResource>()
        where TResource : QualitySizeResource
    {
        var files = registry.GetFiles<TResource>();
        return loader.Load<TResource>(files).Select(tuple => tuple.Resource).ToList();
    }
}
