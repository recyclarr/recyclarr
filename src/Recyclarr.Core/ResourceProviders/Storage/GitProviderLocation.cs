using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public class GitProviderLocation(
    GitResourceProvider config,
    IProviderTypeStrategy strategy,
    IAppPaths appPaths,
    IRepoUpdater updater,
    ILogger log
) : IProviderLocation
{
    public delegate GitProviderLocation Factory(
        GitResourceProvider config,
        IProviderTypeStrategy strategy
    );

    public async Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        var cachePath = appPaths
            .ResourceDirectory.SubDirectory(config.Type)
            .SubDirectory("git")
            .SubDirectory(config.Name);

        progress?.Report(new ProviderProgress(config.Name, ProviderStatus.Processing));

        var references = config.Reference is not null
            ? [config.Reference]
            : strategy.DefaultReferences;

        try
        {
            await updater.UpdateRepo(
                new GitRepositorySource
                {
                    Name = config.Name,
                    CloneUrl = config.CloneUrl,
                    References = references,
                    Path = cachePath,
                },
                ct
            );

            progress?.Report(new ProviderProgress(config.Name, ProviderStatus.Completed));
            return [cachePath];
        }
        catch (Exception e)
        {
            log.Error(e, "Git provider {Name} failed initialization", config.Name);
            progress?.Report(new ProviderProgress(config.Name, ProviderStatus.Failed, e.Message));
            throw;
        }
    }
}
