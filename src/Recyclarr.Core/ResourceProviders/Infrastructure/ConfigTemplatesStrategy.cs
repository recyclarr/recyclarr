using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.ConfigTemplates;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class ConfigTemplatesStrategy(ResourceRegistry<TemplateMetadata> registry)
    : IProviderTypeStrategy
{
    public string Type => "config-templates";

    // The templates repository tags a branch per Recyclarr major version.
    private static readonly int MajorVersion =
        typeof(ConfigTemplatesStrategy).Assembly.GetName().Version?.Major ?? 0;

    public IReadOnlyList<string> DefaultReferences => [$"v{MajorVersion}", "master", "main"];

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    )
    {
        var official = new GitResourceProvider
        {
            Name = "official",
            Type = "config-templates",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
        };

        var myProviders = providers.Where(p => p.Type == Type);

        return myProviders.All(p => !p.ReplaceDefault) ? [official] : [];
    }

    public void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath)
    {
        var includesFile = rootPath.File("includes.json");
        if (!includesFile.Exists)
        {
            return;
        }

        var includesData = DeserializeTemplatesData(includesFile);

        registry.Register<RadarrConfigIncludeResource>(
            includesData.Radarr.Select(e => TemplateMetadata.From(e, rootPath))
        );
        registry.Register<SonarrConfigIncludeResource>(
            includesData.Sonarr.Select(e => TemplateMetadata.From(e, rootPath))
        );
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
