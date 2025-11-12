using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide;

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

internal class ConfigTemplatesResourceQueryAdapter(ConfigTemplatesResourceQuery newQuery)
    : IConfigTemplatesResourceQuery
{
    public IReadOnlyCollection<TemplatePath> GetTemplates()
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
