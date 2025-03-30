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
    GitRepositorySource config,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
)
    : ICustomFormatsResourceProvider,
        IQualitySizeResourceProvider,
        IMediaNamingResourceProvider,
        ICustomFormatCategoriesResourceProvider
{
    private readonly Lazy<RepoMetadata> _metadata = new(() =>
        DeserializeMetadata(
            appPaths
                .ReposDirectory.SubDirectory($"trash-guides-{config.Name ?? "default"}")
                .File("metadata.json")
        )
    );
    private readonly IDirectoryInfo _repoPath = appPaths.ReposDirectory.SubDirectory(
        $"trash-guides-{config.Name ?? "default"}"
    );

    public string Name => config.Name ?? "default";

    public async Task Initialize(CancellationToken token)
    {
        if (config.CloneUrl is null)
        {
            throw new InvalidOperationException(
                "GitRepositorySource must have CloneUrl configured"
            );
        }

        var repoSettings = new GitRepositorySettings
        {
            CloneUrl = config.CloneUrl,
            Branch = config.Reference ?? "master",
            Sha1 = null,
        };

        await repoUpdater.UpdateRepo(_repoPath, repoSettings, token);
    }

    public IEnumerable<IDirectoryInfo> GetCustomFormatPaths(SupportedServices service)
    {
        var metadata = _metadata.Value;
        var relativePaths = service switch
        {
            SupportedServices.Radarr => metadata.JsonPaths.Radarr.CustomFormats,
            SupportedServices.Sonarr => metadata.JsonPaths.Sonarr.CustomFormats,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
        };

        return relativePaths.Select(path => _repoPath.SubDirectory(path));
    }

    public IEnumerable<IDirectoryInfo> GetQualitySizePaths(SupportedServices service)
    {
        var metadata = _metadata.Value;
        var relativePaths = service switch
        {
            SupportedServices.Radarr => metadata.JsonPaths.Radarr.Qualities,
            SupportedServices.Sonarr => metadata.JsonPaths.Sonarr.Qualities,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
        };

        return relativePaths.Select(path => _repoPath.SubDirectory(path));
    }

    public IEnumerable<IDirectoryInfo> GetMediaNamingPaths(SupportedServices service)
    {
        var metadata = _metadata.Value;
        var relativePaths = service switch
        {
            SupportedServices.Radarr => metadata.JsonPaths.Radarr.Naming,
            SupportedServices.Sonarr => metadata.JsonPaths.Sonarr.Naming,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
        };

        return relativePaths.Select(path => _repoPath.SubDirectory(path));
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
