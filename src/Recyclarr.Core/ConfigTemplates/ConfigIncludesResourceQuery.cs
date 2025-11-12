using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

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

internal class ConfigIncludesResourceQueryAdapter(ConfigIncludesResourceQuery newQuery)
    : IConfigIncludesResourceQuery
{
    public IReadOnlyCollection<TemplatePath> GetIncludes()
    {
        var radarr = newQuery
            .GetRadarr()
            .Select(r => new TemplatePath
            {
                Id = r.Id,
                TemplateFile = r.TemplateFile,
                Service = SupportedServices.Radarr,
                Hidden = r.Hidden,
            });

        var sonarr = newQuery
            .GetSonarr()
            .Select(s => new TemplatePath
            {
                Id = s.Id,
                TemplateFile = s.TemplateFile,
                Service = SupportedServices.Sonarr,
                Hidden = s.Hidden,
            });

        return radarr.Concat(sonarr).ToList();
    }
}
