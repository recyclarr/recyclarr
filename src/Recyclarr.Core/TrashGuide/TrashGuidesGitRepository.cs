using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.TrashGuide.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.TrashGuide;

internal class TrashGuidesGitRepository(
    ISettings<ResourceProviderSettings> settings,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths,
    ILogger log
)
    : ICustomFormatsResourceProvider,
        IQualitySizeResourceProvider,
        IMediaNamingResourceProvider,
        ICustomFormatCategoriesResourceProvider
{
    private record ProcessedRepository(IDirectoryInfo RepoPath, RepoMetadata Metadata);

    private readonly Dictionary<string, ProcessedRepository> _processedRepositories = [];

    public string Name => "Git Trash Guides Provider";

    public string GetSourceDescription()
    {
        var repoCount = _processedRepositories.Count;
        var repoNames = string.Join(", ", _processedRepositories.Keys);
        return $"Git Repositories ({repoCount}): {repoNames}";
    }

    public IDirectoryInfo RepoParentPath { get; } =
        appPaths.ReposDirectory.SubDirectory("trash-guides");

    public async Task Initialize(CancellationToken ct)
    {
        // Clean up legacy Git repository if it exists
        LegacyRepositoryCleanup.CleanLegacyRepository(RepoParentPath);

        // Always include official TRaSH Guides repository first so it may be overridden
        var officialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };

        var allRepos = new[] { officialRepo }
            .Concat(settings.Value.TrashGuides.OfType<GitRepositorySource>())
            .ToList();

        log.Debug(
            "TrashGuidesGitRepository.Initialize: Processing {RepoCount} repositories",
            allRepos.Count
        );
        foreach (var repo in allRepos)
        {
            log.Debug(
                "  - Name: '{Name}', CloneUrl: '{CloneUrl}', Reference: '{Reference}'",
                repo.Name,
                repo.CloneUrl,
                repo.Reference
            );
        }

        foreach (var gitRepo in allRepos)
        {
            log.Debug(
                "TrashGuidesGitRepository.Initialize: Processing repo '{Name}' at '{CloneUrl}'",
                gitRepo.Name,
                gitRepo.CloneUrl
            );
            var repoPath = await UpdateSingleRepository(gitRepo, ct);
            var metadata = DeserializeMetadata(repoPath.File("metadata.json"));
            _processedRepositories[gitRepo.Name] = new ProcessedRepository(repoPath, metadata);
            log.Debug(
                "TrashGuidesGitRepository.Initialize: Added repo '{Name}' to _processedRepositories (total: {Total})",
                gitRepo.Name,
                _processedRepositories.Count
            );
        }
    }

    private async Task<IDirectoryInfo> UpdateSingleRepository(
        GitRepositorySource config,
        CancellationToken ct
    )
    {
        var repoPath = RepoParentPath.SubDirectory(config.Name);
        await repoUpdater.UpdateRepo(repoPath, config, ct);
        return repoPath;
    }

    public IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service)
    {
        log.Debug("GetCustomFormatPaths called for service {Service}", service);
        log.Debug(
            "Available repositories: {RepoNames}",
            string.Join(", ", _processedRepositories.Keys)
        );

        var allPaths = new List<IDirectoryInfo>();

        foreach (var repo in _processedRepositories.Values)
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.CustomFormats,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.CustomFormats,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            var repoPaths = relativePaths.Select(path => repo.RepoPath.SubDirectory(path)).ToList();
            log.Debug(
                "Repository '{RepoPath}' contributes {PathCount} paths: {Paths}",
                repo.RepoPath.FullName,
                repoPaths.Count,
                string.Join(", ", repoPaths.Select(p => p.FullName))
            );

            allPaths.AddRange(repoPaths);
        }

        log.Debug("Total custom format paths returned: {TotalCount}", allPaths.Count);
        return allPaths;
    }

    public IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service)
    {
        return _processedRepositories.Values.SelectMany(repo =>
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
        return _processedRepositories.Values.SelectMany(repo =>
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
        if (_processedRepositories.TryGetValue("official", out var officialRepo))
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
            GlobalJsonSerializerSettings.Guide
        );
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
    }
}
