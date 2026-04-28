using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class RepoUpdater(ILogger log, Func<IDirectoryInfo, IGitRepository> repoFactory)
    : IRepoUpdater
{
    private static readonly string[] ValidGitPaths = [".git/config", ".git/index", ".git/HEAD"];

    public async Task UpdateRepo(GitRepositorySource repositorySource, CancellationToken token)
    {
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
        await MaybeRunMaintenanceAsync(repo, repositorySource, token);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Maintenance is best-effort; any failure must not propagate to fail the sync."
    )]
    private async Task MaybeRunMaintenanceAsync(
        IGitRepository repo,
        GitRepositorySource repositorySource,
        CancellationToken token
    )
    {
        var limitBytes = repositorySource.CacheLimit.Bytes;
        if (limitBytes <= 0)
        {
            return;
        }

        var gitDir = repositorySource.Path.SubDirectory(".git");
        if (!gitDir.Exists)
        {
            return;
        }

        var currentSize = gitDir.DirectorySize();
        if (currentSize <= limitBytes)
        {
            return;
        }

        log.Information(
            "Git cache for {Name} is {SizeMb:F1} MB (limit: {LimitMb} MB); running maintenance",
            repositorySource.Name,
            currentSize / (1024.0 * 1024.0),
            limitBytes / (1024 * 1024)
        );

        try
        {
            await repo.RunMaintenance(token);
        }
        catch (Exception e)
        {
            log.Warning(e, "Git cache maintenance failed for {Name}", repositorySource.Name);
        }
    }

    private static void ValidateGitDirectory(IDirectoryInfo repoPath)
    {
        if (ValidGitPaths.Select(repoPath.File).Any(x => !x.Exists))
        {
            throw new InvalidGitRepoException("A `.git` directory or its files are missing");
        }
    }
}
