using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualitySizeResourceQuery(ResourceProviders.Domain.QualitySizeResourceQuery newQuery)
    : IQualitySizeResourceQuery
{
    private readonly Dictionary<SupportedServices, IReadOnlyList<QualitySizeResource>> _cache = [];

    public IReadOnlyList<QualitySizeResource> GetQualitySizeData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        var result = serviceType switch
        {
            SupportedServices.Radarr => newQuery.GetRadarr().Cast<QualitySizeResource>().ToList(),
            SupportedServices.Sonarr => newQuery.GetSonarr().Cast<QualitySizeResource>().ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };

        _cache[serviceType] = result;
        return result;
    }
}
