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

public class GitTrashGuidesResourceProvider(
    ISettings<ResourceProviderSettings> settings,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
)
    : ICustomFormatsResourceProvider,
        IQualitySizeResourceProvider,
        IMediaNamingResourceProvider,
        ICustomFormatCategoriesResourceProvider
{
    private readonly Lazy<Task<Dictionary<string, ProcessedRepository>>> _repositories = new(() =>
        ProcessAllRepositoriesAsync(settings, repoUpdater, appPaths)
    );

    private static async Task<Dictionary<string, ProcessedRepository>> ProcessAllRepositoriesAsync(
        ISettings<ResourceProviderSettings> settings,
        IRepoUpdater repoUpdater,
        IAppPaths appPaths
    )
    {
        var results = new Dictionary<string, ProcessedRepository>();

        foreach (var gitRepo in settings.Value.TrashGuides.OfType<GitRepositorySource>())
        {
            var processed = await ProcessSingleRepository(gitRepo, repoUpdater, appPaths);
            results[gitRepo.Name ?? "default"] = processed;
        }

        return results;
    }

    private static async Task<ProcessedRepository> ProcessSingleRepository(
        GitRepositorySource config,
        IRepoUpdater repoUpdater,
        IAppPaths appPaths
    )
    {
        if (config.CloneUrl is null)
        {
            throw new InvalidOperationException(
                "GitRepositorySource must have CloneUrl configured"
            );
        }

        var repoPath = appPaths.ReposDirectory.SubDirectory(
            $"trash-guides-{config.Name ?? "default"}"
        );

        var repoSettings = new GitRepositorySettings
        {
            CloneUrl = config.CloneUrl,
            Branch = config.Reference ?? "master",
            Sha1 = null,
        };

        await repoUpdater.UpdateRepo(repoPath, repoSettings, CancellationToken.None);

        var metadata = DeserializeMetadata(repoPath.File("metadata.json"));

        return new ProcessedRepository(repoPath, metadata);
    }

    private record ProcessedRepository(IDirectoryInfo RepoPath, RepoMetadata Metadata);

    public string Name => "Git Trash Guides Provider";

    public async Task Initialize(CancellationToken token)
    {
        // Process all repositories - this triggers the lazy initialization
        await _repositories.Value;
    }

    public IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service)
    {
        var repos = _repositories.Value.GetAwaiter().GetResult();
        var allPaths = new List<IDirectoryInfo>();

        foreach (var (name, repo) in repos)
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.CustomFormats,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.CustomFormats,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            allPaths.AddRange(relativePaths.Select(path => repo.RepoPath.SubDirectory(path)));
        }

        return allPaths;
    }

    public IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service)
    {
        var repos = _repositories.Value.GetAwaiter().GetResult();
        var allPaths = new List<IDirectoryInfo>();

        foreach (var (name, repo) in repos)
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.Qualities,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.Qualities,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            allPaths.AddRange(relativePaths.Select(path => repo.RepoPath.SubDirectory(path)));
        }

        return allPaths;
    }

    public IEnumerable<IDirectoryInfo> GetMediaNamingPaths(SupportedServices service)
    {
        var repos = _repositories.Value.GetAwaiter().GetResult();
        var allPaths = new List<IDirectoryInfo>();

        foreach (var (name, repo) in repos)
        {
            var relativePaths = service switch
            {
                SupportedServices.Radarr => repo.Metadata.JsonPaths.Radarr.Naming,
                SupportedServices.Sonarr => repo.Metadata.JsonPaths.Sonarr.Naming,
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
            };

            allPaths.AddRange(relativePaths.Select(path => repo.RepoPath.SubDirectory(path)));
        }

        return allPaths;
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
