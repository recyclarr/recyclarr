using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Repo;

public class RepoUpdater : IRepoUpdater
{
    private readonly ILogger _log;
    private readonly IGitRepositoryFactory _repositoryFactory;
    private readonly IFileUtilities _fileUtils;

    public RepoUpdater(
        ILogger log,
        IGitRepositoryFactory repositoryFactory,
        IFileUtilities fileUtils)
    {
        _log = log;
        _repositoryFactory = repositoryFactory;
        _fileUtils = fileUtils;
    }

    public async Task UpdateRepo(IDirectoryInfo repoPath, IRepositorySettings repoSettings)
    {
        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            await CheckoutAndUpdateRepo(repoPath, repoSettings);
        }
        catch (GitCmdException e)
        {
            _log.Debug(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}", e.ExitCode, e.Error);
            _log.Warning("Deleting local git repo '{Repodir}' and retrying git operation due to error", repoPath.Name);
            _fileUtils.DeleteReadOnlyDirectory(repoPath.FullName);
            await CheckoutAndUpdateRepo(repoPath, repoSettings);
        }
    }

    private async Task CheckoutAndUpdateRepo(IDirectoryInfo repoPath, IRepositorySettings repoSettings)
    {
        var cloneUrl = repoSettings.CloneUrl;
        var branch = repoSettings.Branch;

        _log.Debug("Using Branch & Clone URL: {Branch}, {Url}", branch, cloneUrl);
        if (repoSettings.Sha1 is not null)
        {
            _log.Warning("Using explicit SHA1 for local repository: {Sha1}", repoSettings.Sha1);
        }

        using var repo = await _repositoryFactory.CreateAndCloneIfNeeded(cloneUrl, repoPath, branch);
        await repo.ForceCheckout(branch);

        try
        {
            await repo.Fetch();
        }
        catch (GitCmdException e)
        {
            _log.Debug(e, "Non-zero exit code {ExitCode} while running git fetch: {Error}", e.ExitCode, e.Error);
            _log.Error(
                "Updating the repo '{RepoDir}' (git fetch) failed. Proceeding with existing files. " +
                "Check clone URL is correct and that github is not down",
                repoPath.Name);
        }

        await repo.ResetHard(repoSettings.Sha1 ?? $"origin/{branch}");
    }
}
