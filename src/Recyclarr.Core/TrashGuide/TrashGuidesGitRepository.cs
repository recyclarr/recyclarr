using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.TrashGuide.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.TrashGuide;

internal class TrashGuidesGitRepository(
    ISettings<ResourceProviderSettings> settings,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
)
    : ICustomFormatsResourceProvider,
        IQualitySizeResourceProvider,
        IMediaNamingResourceProvider,
        ICustomFormatCategoriesResourceProvider
{
    private record ProcessedRepository(IDirectoryInfo RepoPath, RepoMetadata Metadata);

    private readonly List<IDirectoryInfo> _repositoryPaths = [];

    public string Name => "Git Trash Guides Provider";

    public IDirectoryInfo RepoParentPath { get; } =
        appPaths.ReposDirectory.SubDirectory("trash-guides");

    public async Task Initialize(CancellationToken ct)
    {
        // Always include official TRaSH Guides repository first so it may be overridden
        var officialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };

        var allRepos = new[] { officialRepo }.Concat(
            settings.Value.TrashGuides.OfType<GitRepositorySource>()
        );

        foreach (var gitRepo in allRepos)
        {
            var repoPath = await UpdateSingleRepository(gitRepo, ct);
            _repositoryPaths.Add(repoPath);
        }
    }

    private async Task<IDirectoryInfo> UpdateSingleRepository(
        GitRepositorySource config,
        CancellationToken ct
    )
    {
        var repoPath = RepoParentPath.SubDirectory(config.Name);

        var repoSettings = new GitRepositorySettings
        {
            CloneUrl = config.CloneUrl!,
            Branch = config.Reference,
            Sha1 = null,
        };

        await repoUpdater.UpdateRepo(repoPath, repoSettings, ct);

        return repoPath;
    }

    private IEnumerable<ProcessedRepository> GetProcessedRepositories()
    {
        return _repositoryPaths.Select(repoPath =>
        {
            var metadata = DeserializeMetadata(repoPath.File("metadata.json"));
            return new ProcessedRepository(repoPath, metadata);
        });
    }

    public IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service)
    {
        return GetProcessedRepositories()
            .SelectMany(repo =>
            {
                var relativePaths = service switch
                {
                    SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.CustomFormats,
                    SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.CustomFormats,
                    _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
                };

                return relativePaths.Select(path => repo.RepoPath.SubDirectory(path));
            });
    }

    public IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service)
    {
        return GetProcessedRepositories()
            .SelectMany(repo =>
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
        return GetProcessedRepositories()
            .SelectMany(repo =>
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

    public ICollection<CustomFormatCategoryItem> GetCategoryData()
    {
        // For now, return empty collection - we'll implement this properly when we integrate the category parser
        // The category parser logic will be moved here from CustomFormatLoader
        return new List<CustomFormatCategoryItem>();
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

    private class GitRepositorySettings : IRepositorySettings
    {
        public Uri CloneUrl { get; init; } = null!;
        public string Branch { get; init; } = "master";
        public string? Sha1 { get; init; }
    }
}
