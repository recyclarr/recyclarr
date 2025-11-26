using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.TrashGuide.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.TrashGuide;

internal abstract class TrashGuidesResourceProvider
    : ICustomFormatsResourceProvider,
        IQualitySizeResourceProvider,
        IMediaNamingResourceProvider,
        ICustomFormatCategoriesResourceProvider
{
    private record ProcessedRepository(IDirectoryInfo RepoPath, RepoMetadata Metadata);

    private readonly Lazy<Dictionary<string, ProcessedRepository>> _processedRepositories;

    protected TrashGuidesResourceProvider()
    {
        _processedRepositories = new Lazy<Dictionary<string, ProcessedRepository>>(
            ProcessRepositories
        );
    }

    public string Name => "TRaSH Guides";

    protected abstract IEnumerable<IDirectoryInfo> GetSourceDirectories();

    private Dictionary<string, ProcessedRepository> ProcessRepositories()
    {
        return GetSourceDirectories()
            .Select(repoPath => (RepoPath: repoPath, MetadataFile: repoPath.File("metadata.json")))
            .Where(x => x.MetadataFile.Exists)
            .ToDictionary(
                x => x.RepoPath.Name,
                x => new ProcessedRepository(x.RepoPath, DeserializeMetadata(x.MetadataFile))
            );
    }

    public IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service)
    {
        var allPaths = new List<IDirectoryInfo>();

        foreach (var repo in _processedRepositories.Value.Values)
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.CustomFormats,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.CustomFormats,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            var repoPaths = relativePaths.Select(path => repo.RepoPath.SubDirectory(path));
            allPaths.AddRange(repoPaths);
        }

        return allPaths;
    }

    public IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service)
    {
        return _processedRepositories.Value.Values.SelectMany(repo =>
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.Qualities,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.Qualities,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            return relativePaths.Select(path => repo.RepoPath.SubDirectory(path));
        });
    }

    public IEnumerable<IDirectoryInfo> GetMediaNamingPaths(SupportedServices service)
    {
        return _processedRepositories.Value.Values.SelectMany(repo =>
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.Naming,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.Naming,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            return relativePaths.Select(path => repo.RepoPath.SubDirectory(path));
        });
    }

    public IFileInfo? GetCategoryMarkdownFile(SupportedServices serviceType)
    {
        var fileName = serviceType switch
        {
            SupportedServices.Radarr => "docs/Radarr/Radarr-collection-of-custom-formats.md",
            SupportedServices.Sonarr => "docs/Sonarr/sonarr-collection-of-custom-formats.md",
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null),
        };

        // Get category file from official repository only (explicit name-based lookup)
        if (_processedRepositories.Value.TryGetValue("official", out var officialRepo))
        {
            var file = officialRepo.RepoPath.File(fileName);
            return file.Exists ? file : null;
        }

        return null;
    }

    private static RepoMetadata DeserializeMetadata(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();

        var obj = JsonSerializer.Deserialize<RepoMetadata>(
            stream,
            GlobalJsonSerializerSettings.Metadata
        );
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
    }
}
