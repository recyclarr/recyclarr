using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(ResourceRegistry<TemplateMetadata> registry)
{
    public IReadOnlyCollection<RadarrConfigTemplateResource> GetRadarr()
    {
        var metadata = registry.Get<RadarrConfigTemplateResource>();
        return metadata
            .Select(m => new RadarrConfigTemplateResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
    }

    public IReadOnlyCollection<SonarrConfigTemplateResource> GetSonarr()
    {
        var metadata = registry.Get<SonarrConfigTemplateResource>();
        return metadata
            .Select(m => new SonarrConfigTemplateResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
    }
}
