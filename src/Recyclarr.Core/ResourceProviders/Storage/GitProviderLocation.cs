using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public class GitProviderLocation(
    GitResourceProvider config,
    IAppPaths appPaths,
    IRepoUpdater updater,
    ILogger log
) : IProviderLocation
{
    public delegate GitProviderLocation Factory(GitResourceProvider config);

    public async Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        var cachePath = appPaths
            .ReposDirectory.SubDirectory(config.Type)
            .SubDirectory("git")
            .SubDirectory(config.Name);

        progress?.Report(new ProviderProgress(config.Type, config.Name, ProviderStatus.Processing));

        try
        {
            await updater.UpdateRepo(
                new GitRepositorySource
                {
                    Name = config.Name,
                    CloneUrl = config.CloneUrl,
                    Reference = config.Reference,
                    Path = cachePath,
                    Depth = config.Depth,
                },
                ct
            );

            progress?.Report(
                new ProviderProgress(config.Type, config.Name, ProviderStatus.Completed)
            );
            return [cachePath];
        }
        catch (Exception e)
        {
            log.Error(e, "Git provider {Name} failed initialization", config.Name);
            progress?.Report(
                new ProviderProgress(config.Type, config.Name, ProviderStatus.Failed, e.Message)
            );
            throw;
        }
    }
}
