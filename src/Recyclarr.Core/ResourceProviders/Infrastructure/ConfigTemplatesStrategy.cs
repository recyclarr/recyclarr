using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.ConfigTemplates;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class ConfigTemplatesStrategy : IProviderTypeStrategy
{
    public string Type => "config-templates";

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        ResourceProviderSettings settings
    )
    {
        var official = new GitResourceProvider
        {
            Name = "official",
            Type = "config-templates",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
            Reference = "master",
        };

        var myProviders = settings.Providers.Where(p => p.Type == Type);

        return myProviders.All(p => !p.ReplaceDefault) ? [official] : [];
    }

    public void MapResourcePaths(
        ResourceProvider config,
        IDirectoryInfo rootPath,
        IResourcePathRegistry registry
    )
    {
        var templatesFile = rootPath.File("templates.json");
        if (templatesFile.Exists)
        {
            var templatesData = DeserializeTemplatesData(templatesFile);

            registry.Register<RadarrConfigTemplateResource>(
                templatesData.Radarr.Select(e => rootPath.File(e.Template))
            );
            registry.Register<SonarrConfigTemplateResource>(
                templatesData.Sonarr.Select(e => rootPath.File(e.Template))
            );
        }

        var includesFile = rootPath.File("includes.json");
        if (includesFile.Exists)
        {
            var includesData = DeserializeTemplatesData(includesFile);

            registry.Register<RadarrConfigIncludeResource>(
                includesData.Radarr.Select(e => rootPath.File(e.Template))
            );
            registry.Register<SonarrConfigIncludeResource>(
                includesData.Sonarr.Select(e => rootPath.File(e.Template))
            );
        }
    }

    private static TemplatesData DeserializeTemplatesData(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();
        return JsonSerializer.Deserialize<TemplatesData>(
                stream,
                GlobalJsonSerializerSettings.Recyclarr
            ) ?? throw new InvalidDataException($"Unable to deserialize {jsonFile}");
    }
}
