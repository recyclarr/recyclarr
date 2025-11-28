using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Serilog;

namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(ResourceRegistry<TemplateMetadata> registry, ILogger log)
{
    public IReadOnlyCollection<RadarrConfigIncludeResource> GetRadarr()
    {
        log.Debug("ConfigIncludes: Querying Radarr config includes");
        var metadata = registry.Get<RadarrConfigIncludeResource>();
        log.Debug("ConfigIncludes: Found {Count} Radarr includes in registry", metadata.Count);

        var result = metadata
            .Select(m => new RadarrConfigIncludeResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug("ConfigIncludes: Retrieved {Count} unique Radarr includes", result.Count);
        return result;
    }

    public IReadOnlyCollection<SonarrConfigIncludeResource> GetSonarr()
    {
        log.Debug("ConfigIncludes: Querying Sonarr config includes");
        var metadata = registry.Get<SonarrConfigIncludeResource>();
        log.Debug("ConfigIncludes: Found {Count} Sonarr includes in registry", metadata.Count);

        var result = metadata
            .Select(m => new SonarrConfigIncludeResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        log.Debug("ConfigIncludes: Retrieved {Count} unique Sonarr includes", result.Count);
        return result;
    }
}
