using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Settings;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class RepoUpdater : IRepoUpdater
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly IGitRepositoryFactory _repositoryFactory;
    private readonly IFileUtilities _fileUtils;
    private readonly ISettingsProvider _settingsProvider;

    public RepoUpdater(
        ILogger log,
        IAppPaths paths,
        IGitRepositoryFactory repositoryFactory,
        IFileUtilities fileUtils,
        ISettingsProvider settingsProvider)
    {
        _log = log;
        _paths = paths;
        _repositoryFactory = repositoryFactory;
        _fileUtils = fileUtils;
        _settingsProvider = settingsProvider;
    }

    public IDirectoryInfo RepoPath => _paths.RepoDirectory;

    public async Task UpdateRepo()
    {
        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            await CheckoutAndUpdateRepo();
        }
        catch (GitCmdException e)
        {
            _log.Debug(e, "Non-zero exit code {ExitCode} while executing Git command: {Error}", e.ExitCode, e.Error);
            _log.Warning("Deleting local git repo and retrying git operation due to error...");
            _fileUtils.DeleteReadOnlyDirectory(RepoPath.FullName);
            await CheckoutAndUpdateRepo();
        }
    }

    private async Task CheckoutAndUpdateRepo()
    {
        var repoSettings = _settingsProvider.Settings.Repository;
        var cloneUrl = repoSettings.CloneUrl;
        var branch = repoSettings.Branch;

        _log.Debug("Using Branch & Clone URL: {Branch}, {Url}", branch, cloneUrl);
        if (repoSettings.Sha1 is not null)
        {
            _log.Warning("Using explicit SHA1 for local repository: {Sha1}", repoSettings.Sha1);
        }

        using var repo = await _repositoryFactory.CreateAndCloneIfNeeded(cloneUrl, RepoPath.FullName, branch);
        await repo.ForceCheckout(branch);
        await repo.Fetch();
        await repo.ResetHard(repoSettings.Sha1 ?? $"origin/{branch}");
    }
}
