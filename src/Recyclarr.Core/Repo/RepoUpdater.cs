using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class RepoUpdater(ILogger log, Func<IDirectoryInfo, IGitRepository> repoFactory)
    : IRepoUpdater
{
    private const double BytesPerMegabyte = 1024.0 * 1024.0;
    private static readonly string[] ValidGitPaths = [".git/config", ".git/index", ".git/HEAD"];

    public async Task UpdateRepo(GitRepositorySource repositorySource, CancellationToken token)
    {
        // Assume failure until it succeeds, to simplify the catch handlers.
        var succeeded = false;
        var rebuildLegacyCache = false;

        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            rebuildLegacyCache = await CheckoutAndUpdateRepo(repositorySource, token);
            succeeded = !rebuildLegacyCache;

            if (rebuildLegacyCache)
            {
                log.Information(
                    "Rebuilding legacy git cache for {Name} because stale references prevent storage cleanup",
                    repositorySource.Name
                );
            }
        }
        catch (GitCmdException e)
        {
            log.Debug(e, "Non-zero exit code {ExitCode} while executing Git command", e.ExitCode);
        }
        catch (InvalidGitRepoException e)
        {
            log.Debug(e, "Git repository is not valid (missing files in `.git` directory)");
        }

        if (!succeeded)
        {
            if (!rebuildLegacyCache)
            {
                log.Warning("Deleting local git repo and retrying git operation due to error");
            }

            repositorySource.Path.DeleteReadOnlyDirectory();
            _ = await CheckoutAndUpdateRepo(repositorySource, token);
        }
    }

    // Returns whether the cache must be rebuilt to remove legacy Git references.
    private async Task<bool> CheckoutAndUpdateRepo(
        GitRepositorySource repositorySource,
        CancellationToken token
    )
    {
        var cloneUrl = repositorySource.CloneUrl;
        var references = repositorySource.References;
        var repoPath = repositorySource.Path;

        log.Debug("Using URL: {Url}, Refs: {@References}", cloneUrl, references);

        using var repo = repoFactory(repoPath);

        if (!repoPath.Exists)
        {
            log.Debug("Initializing new repository at {RepoPath}", repoPath.FullName);
            await repo.Init(token);
        }
        else
        {
            ValidateGitDirectory(repoPath);
            await repo.Status(token);
        }

        try
        {
            await repo.Fetch(cloneUrl, references, token, ["--depth", "1"]);
        }
        catch (AggregateException e)
        {
            log.Warning(
                e,
                "All references failed to fetch (will proceed with existing files): {@References}",
                references
            );
        }

        await repo.ResetHard("FETCH_HEAD", token);
        return await MaybeRunMaintenanceAsync(repo, repositorySource, token);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maintenance is best-effort; any failure must not propagate to fail the sync."
    )]
    private async Task<bool> MaybeRunMaintenanceAsync(
        IGitRepository repo,
        GitRepositorySource repositorySource,
        CancellationToken token
    )
    {
        var limitBytes = repositorySource.CacheLimit.Bytes;
        if (limitBytes <= 0)
        {
            return false;
        }

        var gitDir = repositorySource.Path.SubDirectory(".git");
        if (!gitDir.Exists)
        {
            return false;
        }

        try
        {
            var currentSize = gitDir.DirectorySize();
            if (currentSize <= limitBytes)
            {
                return false;
            }

            if (await repo.HasRemoteReferences(token))
            {
                return true;
            }

            log.Information(
                "Git cache for {Name} is {SizeMb:F1} MB (limit: {LimitMb:F1} MB); running maintenance",
                repositorySource.Name,
                currentSize / BytesPerMegabyte,
                limitBytes / BytesPerMegabyte
            );

            await repo.RunMaintenance(token);

            var finalSize = gitDir.DirectorySize();
            log.Information(
                "Git cache maintenance for {Name} completed: {PreviousSizeMb:F1} MB -> {FinalSizeMb:F1} MB",
                repositorySource.Name,
                currentSize / BytesPerMegabyte,
                finalSize / BytesPerMegabyte
            );

            if (finalSize > limitBytes)
            {
                log.Warning(
                    "Git cache for {Name} remains above the cleanup threshold: {SizeMb:F1} MB (limit: {LimitMb:F1} MB)",
                    repositorySource.Name,
                    finalSize / BytesPerMegabyte,
                    limitBytes / BytesPerMegabyte
                );
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            log.Warning(e, "Git cache maintenance failed for {Name}", repositorySource.Name);
        }

        return false;
    }

    private static void ValidateGitDirectory(IDirectoryInfo repoPath)
    {
        if (ValidGitPaths.Select(repoPath.File).Any(x => !x.Exists))
        {
            throw new InvalidGitRepoException("A `.git` directory or its files are missing");
        }
    }
}
