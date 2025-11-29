using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(ResourceRegistry<TemplateMetadata> registry, ILogger log)
{
    public IReadOnlyCollection<RadarrConfigTemplateResource> GetRadarr()
    {
        log.Debug("ConfigTemplates: Querying Radarr config templates");
        var metadata = registry.Get<RadarrConfigTemplateResource>();
        log.Debug("ConfigTemplates: Found {Count} Radarr templates in registry", metadata.Count);

        var result = metadata
            .Select(m => new RadarrConfigTemplateResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug("ConfigTemplates: Retrieved {Count} unique Radarr templates", result.Count);
        return result;
    }

    public IReadOnlyCollection<SonarrConfigTemplateResource> GetSonarr()
    {
        log.Debug("ConfigTemplates: Querying Sonarr config templates");
        var metadata = registry.Get<SonarrConfigTemplateResource>();
        log.Debug("ConfigTemplates: Found {Count} Sonarr templates in registry", metadata.Count);

        var result = metadata
            .Select(m => new SonarrConfigTemplateResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug("ConfigTemplates: Retrieved {Count} unique Sonarr templates", result.Count);
        return result;
    }
}
