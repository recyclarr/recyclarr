using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class QualityProfileGroupResourceQuery(ResourceRegistry<IFileInfo> registry, ILogger log)
{
    public IReadOnlyList<QualityProfileGroupResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetGroups<RadarrQualityProfileGroupResource>(serviceType),
            SupportedServices.Sonarr => GetGroups<SonarrQualityProfileGroupResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    // Each file is a JSON array of group entries, so we deserialize as a list and flatten
    private List<QualityProfileGroupResource> GetGroups<TResource>(SupportedServices serviceType)
        where TResource : QualityProfileGroupResource
    {
        log.Debug("QualityProfileGroup: Querying {Service} quality profile groups", serviceType);
        var files = registry.Get<TResource>();
        log.Debug(
            "QualityProfileGroup: Found {Count} quality profile group files in registry",
            files.Count
        );

        var result = files
            .SelectMany(LoadGroupsFromFile<TResource>)
            .Cast<QualityProfileGroupResource>()
            .ToList();

        log.Debug(
            "QualityProfileGroup: Retrieved {Count} {Service} quality profile groups",
            result.Count,
            serviceType
        );
        return result;
    }

    private static List<TResource> LoadGroupsFromFile<TResource>(IFileInfo file)
        where TResource : QualityProfileGroupResource
    {
        using var stream = file.OpenRead();
        return JsonSerializer.Deserialize<List<TResource>>(
                stream,
                GlobalJsonSerializerSettings.Metadata
            ) ?? [];
    }
}
