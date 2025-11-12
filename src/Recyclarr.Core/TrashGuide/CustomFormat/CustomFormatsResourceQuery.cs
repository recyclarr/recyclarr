using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatsResourceQuery(CustomFormatResourceQuery newQuery)
    : ICustomFormatsResourceQuery
{
    private readonly Dictionary<SupportedServices, CustomFormatDataResult> _cache = [];

    public CustomFormatDataResult GetCustomFormatData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        var formats = serviceType switch
        {
            SupportedServices.Radarr => newQuery.GetRadarr().Cast<CustomFormatResource>().ToList(),
            SupportedServices.Sonarr => newQuery.GetSonarr().Cast<CustomFormatResource>().ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };

        var result = new CustomFormatDataResult(formats);
        _cache[serviceType] = result;
        return result;
    }
}
