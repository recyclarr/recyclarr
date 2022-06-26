using System.IO.Abstractions;
using Common;
using LibGit2Sharp;
using Serilog;
using TrashLib.Config.Settings;
using VersionControl;

namespace TrashLib.Repo;

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

    public void UpdateRepo()
    {
        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        var exception = CheckoutAndUpdateRepo();
        if (exception is not null)
        {
            _log.Information("Deleting local git repo and retrying git operation...");
            _fileUtils.DeleteReadOnlyDirectory(RepoPath.FullName);

            exception = CheckoutAndUpdateRepo();
            if (exception is not null)
            {
                throw exception;
            }
        }
    }

    private Exception? CheckoutAndUpdateRepo()
    {
        var repoSettings = _settingsProvider.Settings.Repository;
        var cloneUrl = repoSettings.CloneUrl;
        const string branch = "master";

        _log.Debug("Using Branch & Clone URL: {Branch}, {Url}", branch, cloneUrl);

        try
        {
            using var repo = _repositoryFactory.CreateAndCloneIfNeeded(cloneUrl, RepoPath.FullName, branch);
            repo.ForceCheckout(branch);
            repo.Fetch();
            repo.ResetHard($"origin/{branch}");
        }
        catch (LibGit2SharpException e)
        {
            _log.Error(e, "An exception occurred during git operations on path: {RepoPath}", RepoPath);
            return e;
        }

        return null;
    }
}
