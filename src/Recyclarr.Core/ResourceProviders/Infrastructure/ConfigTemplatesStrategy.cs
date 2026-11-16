using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.ConfigTemplates;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class ConfigTemplatesStrategy : IProviderTypeStrategy
{
    public string Type => "config-templates";

    // Template repositories may keep a branch per Recyclarr major version.
    public IReadOnlyList<string> DefaultReferences =>
        [$"v{RecyclarrVersion.Major}", "master", "main"];

    // No official provider: the server generates config templates from guide data, so this type
    // only serves includes from user-configured repositories.
    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    ) => [];

    public void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath, GuideIndex index)
    {
        var includesFile = rootPath.File("includes.json");
        if (!includesFile.Exists)
        {
            return;
        }

        var includesData = DeserializeTemplatesData(includesFile);

        index.Templates.Register<RadarrConfigIncludeResource>(
            includesData.Radarr.Select(e => TemplateMetadata.From(e, rootPath))
        );
        index.Templates.Register<SonarrConfigIncludeResource>(
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
