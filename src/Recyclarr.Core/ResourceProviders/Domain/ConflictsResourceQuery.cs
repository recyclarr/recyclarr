using Recyclarr.Json;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Domain;

public class ConflictsResourceQuery(ResourceScope resources, JsonResourceLoader loader)
{
    /// <summary>
    /// Returns each set of mutually exclusive custom format trash_ids. Sets with fewer than two
    /// members cannot conflict and are omitted.
    /// </summary>
    public IReadOnlyList<IReadOnlySet<string>> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetConflicts<RadarrConflictsResource>(),
            SupportedServices.Sonarr => GetConflicts<SonarrConflictsResource>(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<IReadOnlySet<string>> GetConflicts<TResource>()
        where TResource : ConflictsResource
    {
        var files = resources.Files.Get<TResource>();
        return loader
            .Load<TResource>(files, GlobalJsonSerializerSettings.Metadata)
            .SelectMany(tuple => tuple.Resource.CustomFormats)
            .Where(entry => entry.Count >= 2)
            .Select(entry => (IReadOnlySet<string>)entry.Keys.ToHashSet())
            .ToList();
    }
}
