using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(IResourcePathRegistry registry)
{
    public IReadOnlyCollection<RadarrConfigIncludeResource> GetRadarr()
    {
        var files = registry.GetFiles<RadarrConfigIncludeResource>();
        return files
            .Select(f => new RadarrConfigIncludeResource
            {
                Id = Path.GetFileNameWithoutExtension(f.Name),
                TemplateFile = f,
            })
            .ToList();
    }

    public IReadOnlyCollection<SonarrConfigIncludeResource> GetSonarr()
    {
        var files = registry.GetFiles<SonarrConfigIncludeResource>();
        return files
            .Select(f => new SonarrConfigIncludeResource
            {
                Id = Path.GetFileNameWithoutExtension(f.Name),
                TemplateFile = f,
            })
            .ToList();
    }
}
