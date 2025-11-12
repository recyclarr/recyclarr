using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class TrashGuidesStrategy(IFileSystem fs) : IProviderTypeStrategy
{
    public string Type => "trash-guides";

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        ResourceProviderSettings settings
    )
    {
        var official = new GitResourceProvider
        {
            Name = "official",
            Type = "trash-guides",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
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
        var metadataFile = rootPath.File("metadata.json");
        if (!metadataFile.Exists)
        {
            throw new InvalidDataException($"Provider {config.Name}: metadata.json not found");
        }

        var metadata = DeserializeMetadata(metadataFile);

        var radarrCategoryFile = rootPath.File(
            "docs/Radarr/Radarr-collection-of-custom-formats.md"
        );
        if (radarrCategoryFile.Exists)
        {
            registry.Register<RadarrCategoryMarkdownResource>([radarrCategoryFile]);
        }

        var sonarrCategoryFile = rootPath.File(
            "docs/Sonarr/sonarr-collection-of-custom-formats.md"
        );
        if (sonarrCategoryFile.Exists)
        {
            registry.Register<SonarrCategoryMarkdownResource>([sonarrCategoryFile]);
        }

        registry.Register<RadarrCustomFormatResource>(
            GlobJsonFiles(metadata.JsonPaths.Radarr.CustomFormats, rootPath)
        );
        registry.Register<RadarrQualitySizeResource>(
            GlobJsonFiles(metadata.JsonPaths.Radarr.Qualities, rootPath)
        );
        registry.Register<RadarrMediaNamingResource>(
            GlobJsonFiles(metadata.JsonPaths.Radarr.Naming, rootPath)
        );

        registry.Register<SonarrCustomFormatResource>(
            GlobJsonFiles(metadata.JsonPaths.Sonarr.CustomFormats, rootPath)
        );
        registry.Register<SonarrQualitySizeResource>(
            GlobJsonFiles(metadata.JsonPaths.Sonarr.Qualities, rootPath)
        );
        registry.Register<SonarrMediaNamingResource>(
            GlobJsonFiles(metadata.JsonPaths.Sonarr.Naming, rootPath)
        );
    }

    private IEnumerable<IFileInfo> GlobJsonFiles(
        IEnumerable<string> relativePaths,
        IDirectoryInfo rootPath
    )
    {
        return relativePaths
            .Select(relPath =>
                rootPath.FileSystem.DirectoryInfo.New(
                    rootPath.FileSystem.Path.Combine(rootPath.FullName, relPath)
                )
            )
            .Where(dir => dir.Exists)
            .SelectMany(dir => dir.EnumerateFiles("*.json", SearchOption.AllDirectories));
    }

    private static RepoMetadata DeserializeMetadata(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();
        return JsonSerializer.Deserialize<RepoMetadata>(stream, GlobalJsonSerializerSettings.Guide)
            ?? throw new InvalidDataException($"Unable to deserialize {jsonFile}");
    }
}
