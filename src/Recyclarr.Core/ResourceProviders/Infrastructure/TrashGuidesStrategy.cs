using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class TrashGuidesStrategy(ResourceRegistry<IFileInfo> registry, ILogger log)
    : IProviderTypeStrategy
{
    public string Type => "trash-guides";

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    )
    {
        var official = new GitResourceProvider
        {
            Name = "official",
            Type = "trash-guides",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };

        var myProviders = providers.Where(p => p.Type == Type);

        return myProviders.All(p => !p.ReplaceDefault) ? [official] : [];
    }

    public void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath)
    {
        log.Debug(
            "MapResourcePaths called for provider {Name} at {Path}",
            config.Name,
            rootPath.FullName
        );

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

        registry.Register<RadarrCfGroupResource>(
            GlobJsonFiles(metadata.JsonPaths.Radarr.CfGroups, rootPath)
        );
        registry.Register<SonarrCfGroupResource>(
            GlobJsonFiles(metadata.JsonPaths.Sonarr.CfGroups, rootPath)
        );

        registry.Register<RadarrQualityProfileResource>(
            GlobJsonFiles(metadata.JsonPaths.Radarr.QualityProfiles, rootPath)
        );
        registry.Register<SonarrQualityProfileResource>(
            GlobJsonFiles(metadata.JsonPaths.Sonarr.QualityProfiles, rootPath)
        );
    }

    private IEnumerable<IFileInfo> GlobJsonFiles(
        IEnumerable<string> relativePaths,
        IDirectoryInfo rootPath
    )
    {
        var pathsList = relativePaths.ToList();
        log.Debug(
            "Provider {ProviderRoot}: GlobJsonFiles received {Count} paths",
            rootPath.Name,
            pathsList.Count
        );

        foreach (var relPath in pathsList)
        {
            var fullPath = rootPath.FileSystem.Path.Combine(rootPath.FullName, relPath);
            var dir = rootPath.FileSystem.DirectoryInfo.New(fullPath);

            log.Debug(
                "Provider {ProviderRoot}: Checking {RelPath} (Exists: {Exists})",
                rootPath.Name,
                relPath,
                dir.Exists
            );

            if (dir.Exists)
            {
                var files = dir.EnumerateFiles("*.json", SearchOption.AllDirectories).ToList();
                log.Debug(
                    "Provider {ProviderRoot}: Found {Count} files in {RelPath}",
                    rootPath.Name,
                    files.Count,
                    relPath
                );
                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }

    private static RepoMetadata DeserializeMetadata(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();
        return JsonSerializer.Deserialize<RepoMetadata>(
                stream,
                GlobalJsonSerializerSettings.Metadata
            ) ?? throw new InvalidDataException($"Unable to deserialize {jsonFile}");
    }
}
