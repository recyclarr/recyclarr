using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

[SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Cache rebuild is best-effort; size/delete failures must not fail the sync."
)]
public class RepoUpdater(ILogger log, Func<IDirectoryInfo, IGitRepository> repoFactory)
    : IRepoUpdater
{
    private static readonly string[] ValidGitPaths = [".git/config", ".git/index", ".git/HEAD"];

    public async Task UpdateRepo(GitRepositorySource repositorySource, CancellationToken token)
    {
        var rebuilt = MaybeDeleteForRebuild(repositorySource);

        // Assume failure until it succeeds, to simplify the catch handlers.
        var succeeded = false;

        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            await CheckoutAndUpdateRepo(repositorySource, token);
            succeeded = true;
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
            log.Warning("Deleting local git repo and retrying git operation due to error");
            repositorySource.Path.DeleteReadOnlyDirectory();
            await CheckoutAndUpdateRepo(repositorySource, token);
        }

        if (rebuilt)
        {
            CheckRebuildSize(repositorySource);
        }
    }

    private bool MaybeDeleteForRebuild(GitRepositorySource repositorySource)
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

            log.Information(
                "Git cache for {Name} is {SizeMb:F1} MB (limit: {LimitMb:F1} MB); rebuilding cache",
                repositorySource.Name,
                currentSize / (1024.0 * 1024.0),
                limitBytes / (1024.0 * 1024.0)
            );

            repositorySource.Path.DeleteReadOnlyDirectory();
            return true;
        }
        catch (Exception e)
        {
            log.Warning(
                e,
                "Failed to delete git cache for {Name}; proceeding with existing cache",
                repositorySource.Name
            );
            return false;
        }
    }

    private void CheckRebuildSize(GitRepositorySource repositorySource)
    {
        var limitBytes = repositorySource.CacheLimit.Bytes;
        var gitDir = repositorySource.Path.SubDirectory(".git");
        if (!gitDir.Exists)
        {
            return;
        }

        try
        {
            var newSize = gitDir.DirectorySize();
            log.Information(
                "Git cache for {Name} after rebuild: {SizeMb:F1} MB",
                repositorySource.Name,
                newSize / (1024.0 * 1024.0)
            );

            if (newSize > limitBytes)
            {
                log.Warning(
                    "Git cache for {Name} is {SizeMb:F1} MB after a fresh fetch, which still exceeds "
                        + "the configured limit of {LimitMb:F1} MB. The limit is below the minimum "
                        + "achievable size; consider raising cache_limit.",
                    repositorySource.Name,
                    newSize / (1024.0 * 1024.0),
                    limitBytes / (1024.0 * 1024.0)
                );
            }
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to measure rebuilt git cache for {Name}", repositorySource.Name);
        }
    }

    private async Task CheckoutAndUpdateRepo(
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
    }

    private static void ValidateGitDirectory(IDirectoryInfo repoPath)
    {
        if (ValidGitPaths.Select(repoPath.File).Any(x => !x.Exists))
        {
            throw new InvalidGitRepoException("A `.git` directory or its files are missing");
        }
    }
}
