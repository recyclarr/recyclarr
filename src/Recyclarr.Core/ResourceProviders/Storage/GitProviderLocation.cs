using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Settings.Models;
using Recyclarr.VersionControl;

namespace Recyclarr.ResourceProviders.Storage;

public class GitProviderLocation : IResourceSource
{
    // Kept alive by this ref so its objects survive later shallow fetches.
    private const string GoodRef = "refs/recyclarr/last-good";

    private readonly GitResourceProvider _config;
    private readonly IRepoUpdater _updater;
    private readonly Func<IDirectoryInfo, IGitRepository> _repoFactory;
    private readonly ILogger _log;
    private readonly IDirectoryInfo _cachePath;
    private readonly IReadOnlyList<string> _references;

    public delegate GitProviderLocation Factory(
        GitResourceProvider config,
        IProviderTypeStrategy strategy
    );

    public GitProviderLocation(
        GitResourceProvider config,
        IProviderTypeStrategy strategy,
        IAppPaths appPaths,
        IRepoUpdater updater,
        Func<IDirectoryInfo, IGitRepository> repoFactory,
        ILogger log
    )
    {
        _config = config;
        _updater = updater;
        _repoFactory = repoFactory;
        _log = log;
        Strategy = strategy;

        var gitDir = appPaths.ResourceDirectory.SubDirectory(config.Type).SubDirectory("git");
        _cachePath = gitDir.SubDirectory(config.Name);

        // Without an explicit reference, the strategy's fallback list is the identity, so a
        // Recyclarr major upgrade that changes the list forces a refresh.
        _references = config.Reference is not null
            ? [config.Reference]
            : strategy.DefaultReferences;
        Tracking = new SourceTracking(
            gitDir.File($"{config.Name}.state.json"),
            $"{config.CloneUrl} [{string.Join(", ", _references)}]"
        );
    }

    public ResourceProvider Config => _config;
    public IProviderTypeStrategy Strategy { get; }
    public SourceTracking? Tracking { get; }

    public IReadOnlyCollection<IDirectoryInfo> Roots
    {
        get
        {
            // DirectoryInfo caches Exists; an update may have created or deleted the directory.
            _cachePath.Refresh();
            return _cachePath.Exists ? [_cachePath] : [];
        }
    }

    private bool HasRepo => _cachePath.SubDirectory(".git").Exists;

    public async Task<SourceUpdateOutcome> UpdateAsync(CancellationToken ct)
    {
        try
        {
            var before = await HeadAsync(ct);
            var outcome = await _updater.UpdateRepo(
                new GitRepositorySource
                {
                    Name = _config.Name,
                    CloneUrl = _config.CloneUrl,
                    References = _references,
                    Path = _cachePath,
                    CacheLimit = _config.CacheLimit,
                },
                ct
            );

            if (outcome == RepoUpdateOutcome.FetchFailedUsingCache)
            {
                return SourceUpdateOutcome.FailedUsingCache;
            }

            var after = await HeadAsync(ct);
            return before is not null && before == after
                ? SourceUpdateOutcome.Unchanged
                : SourceUpdateOutcome.Updated;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _log.Error(e, "Git provider {Name} failed to update", _config.Name);
            throw;
        }
    }

    public async Task MarkGoodAsync(CancellationToken ct)
    {
        if (!HasRepo)
        {
            return;
        }

        using var repo = _repoFactory(_cachePath);
        var head =
            await repo.ResolveCommit("HEAD", ct)
            ?? throw new InvalidOperationException($"Git provider {_config.Name} has no HEAD");
        await repo.UpdateRef(GoodRef, head, ct);
    }

    public async Task<bool> RevertToGoodAsync(CancellationToken ct)
    {
        if (!HasRepo)
        {
            return false;
        }

        using var repo = _repoFactory(_cachePath);
        var good = await repo.ResolveCommit(GoodRef, ct);
        if (good is null)
        {
            return false;
        }

        await repo.ResetHard(good, ct);
        return true;
    }

    // Git is not run before a repository exists: running it would create the directory, which
    // the updater then treats as a corrupt repository.
    private async Task<string?> HeadAsync(CancellationToken ct)
    {
        if (!HasRepo)
        {
            return null;
        }

        using var repo = _repoFactory(_cachePath);
        return await repo.ResolveCommit("HEAD", ct);
    }
}
