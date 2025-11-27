using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(ResourceRegistry<TemplateMetadata> registry)
{
    public IReadOnlyCollection<RadarrConfigIncludeResource> GetRadarr()
    {
        var metadata = registry.Get<RadarrConfigIncludeResource>();
        return metadata
            .Select(m => new RadarrConfigIncludeResource
            {
                Id = m.Id,
                TemplateFile = m.TemplateFile,
                Hidden = m.Hidden,
            })
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
    }

    public IReadOnlyCollection<SonarrConfigIncludeResource> GetSonarr()
    {
        var metadata = registry.Get<SonarrConfigIncludeResource>();
        return metadata
            .Select(m => new SonarrConfigIncludeResource
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
