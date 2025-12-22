using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(ResourceRegistry<TemplateMetadata> registry, ILogger log)
{
    public IReadOnlyCollection<ConfigIncludeResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetIncludes<RadarrConfigIncludeResource>(serviceType),
            SupportedServices.Sonarr => GetIncludes<SonarrConfigIncludeResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetIncludes<TResource>(SupportedServices serviceType)
        where TResource : ConfigIncludeResource, new()
    {
        log.Debug("ConfigIncludes: Querying {Service} config includes", serviceType);
        var metadata = registry.Get<TResource>();
        log.Debug(
            "ConfigIncludes: Found {Count} {Service} includes in registry",
            metadata.Count,
            serviceType
        );

        var result = metadata
            .Select(m => new TResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug(
            "ConfigIncludes: Retrieved {Count} unique {Service} includes",
            result.Count,
            serviceType
        );
        return result;
    }
}
