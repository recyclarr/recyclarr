using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(ResourceRegistry<TemplateMetadata> registry, ILogger log)
{
    public IReadOnlyCollection<ConfigTemplateResource> Get(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => GetTemplates<RadarrConfigTemplateResource>(serviceType),
            SupportedServices.Sonarr => GetTemplates<SonarrConfigTemplateResource>(serviceType),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };
    }

    private List<TResource> GetTemplates<TResource>(SupportedServices serviceType)
        where TResource : ConfigTemplateResource, new()
    {
        log.Debug("ConfigTemplates: Querying {Service} config templates", serviceType);
        var metadata = registry.Get<TResource>();
        log.Debug(
            "ConfigTemplates: Found {Count} {Service} templates in registry",
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
            "ConfigTemplates: Retrieved {Count} unique {Service} templates",
            result.Count,
            serviceType
        );
        return result;
    }
}
