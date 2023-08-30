using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Repo;

public class RepoUpdater : IRepoUpdater
{
    private readonly ILogger _log;
    private readonly IGitRepositoryFactory _repositoryFactory;

    public RepoUpdater(ILogger log, IGitRepositoryFactory repositoryFactory)
    {
        _log = log;
        _repositoryFactory = repositoryFactory;
    }

    public async Task UpdateRepo(
        IDirectoryInfo repoPath,
        IRepositorySettings repoSettings,
        CancellationToken token)
    {
        // Assume failure until it succeeds, to simplify the catch handlers.
        var succeeded = false;

        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            await CheckoutAndUpdateRepo(repoPath, repoSettings, token);
            succeeded = true;
        }
        catch (GitCmdException e)
        {
            _log.Debug(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}", e.ExitCode, e.Error);
        }
        catch (InvalidGitRepoException e)
        {
            _log.Debug(e, "Git repository is not valid (missing files in `.git` directory)");
        }

        if (!succeeded)
        {
            _log.Warning("Deleting local git repo and retrying git operation due to error");
            repoPath.DeleteReadOnlyDirectory();
            await CheckoutAndUpdateRepo(repoPath, repoSettings, token);
        }
    }

    private async Task CheckoutAndUpdateRepo(
        IDirectoryInfo repoPath,
        IRepositorySettings repoSettings,
        CancellationToken token)
    {
        var cloneUrl = repoSettings.CloneUrl;
        var branch = repoSettings.Branch;

        _log.Debug("Using Branch & Clone URL: {Branch}, {Url}", branch, cloneUrl);
        if (repoSettings.Sha1 is not null)
        {
            _log.Warning("Using explicit SHA1 for local repository: {Sha1}", repoSettings.Sha1);
        }

        using var repo = await _repositoryFactory.CreateAndCloneIfNeeded(cloneUrl, repoPath, branch, token);
        await repo.ForceCheckout(token, branch);

        try
        {
            await repo.Fetch(token);
        }
        catch (GitCmdException e)
        {
            _log.Debug(e, "Non-zero exit code {ExitCode} while running git fetch: {Error}", e.ExitCode, e.Error);
            _log.Error(
                "Updating the repo '{RepoDir}' (git fetch) failed. Proceeding with existing files. " +
                "Check clone URL is correct and that github is not down",
                repoPath.Name);
        }

        await repo.ResetHard(token, repoSettings.Sha1 ?? $"origin/{branch}");
    }
}
