using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(IResourcePathRegistry registry)
{
    public IReadOnlyCollection<RadarrConfigTemplateResource> GetRadarr()
    {
        var files = registry.GetFiles<RadarrConfigTemplateResource>();
        return files
            .Select(f => new RadarrConfigTemplateResource
            {
                Id = Path.GetFileNameWithoutExtension(f.Name),
                TemplateFile = f,
            })
            .ToList();
    }

    public IReadOnlyCollection<SonarrConfigTemplateResource> GetSonarr()
    {
        var files = registry.GetFiles<SonarrConfigTemplateResource>();
        return files
            .Select(f => new SonarrConfigTemplateResource
            {
                Id = Path.GetFileNameWithoutExtension(f.Name),
                TemplateFile = f,
            })
            .ToList();
    }
}
