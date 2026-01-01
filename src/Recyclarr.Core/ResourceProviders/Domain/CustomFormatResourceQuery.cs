using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class CustomFormatResourceQuery(
    ResourceRegistry<IFileInfo> registry,
    JsonResourceLoader loader,
    ILogger log
)
{
    public IReadOnlyList<CustomFormatResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetCustomFormats<RadarrCustomFormatResource>(serviceType),
            SupportedServices.Sonarr => GetCustomFormats<SonarrCustomFormatResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetCustomFormats<TResource>(SupportedServices serviceType)
        where TResource : CustomFormatResource
    {
        log.Debug("CustomFormat: Querying {Service} custom formats", serviceType);
        var files = registry.Get<TResource>();
        log.Debug("CustomFormat: Found {Count} CF files in registry", files.Count);

        var loaded = loader.Load<TResource>(files, GlobalJsonSerializerSettings.Guide);

        var result = loaded
            .Select(tuple => tuple.Resource with { SourceFile = tuple.SourceFile })
            .GroupBy(cf => cf.TrashId)
            .Select(g => g.Last())
            .ToList();

        log.Debug(
            "CustomFormat: Retrieved {Count} {Service} custom formats",
            result.Count,
            serviceType
        );
        return result;
    }
}
