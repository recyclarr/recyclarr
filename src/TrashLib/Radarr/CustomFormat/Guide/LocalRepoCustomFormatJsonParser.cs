using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Common;
using LibGit2Sharp;
using Serilog;
using TrashLib.Config.Settings;
using TrashLib.Radarr.Config;
using VersionControl;

namespace TrashLib.Radarr.CustomFormat.Guide;

internal class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly ILogger _log;
    private readonly IFileSystem _fileSystem;
    private readonly IGitRepositoryFactory _repositoryFactory;
    private readonly IFileUtilities _fileUtils;
    private readonly ISettingsProvider _settingsProvider;
    private readonly string _repoPath;

    public LocalRepoCustomFormatJsonParser(
        ILogger log,
        IFileSystem fileSystem,
        IResourcePaths paths,
        IGitRepositoryFactory repositoryFactory,
        IFileUtilities fileUtils,
        ISettingsProvider settingsProvider)
    {
        _log = log;
        _fileSystem = fileSystem;
        _repositoryFactory = repositoryFactory;
        _fileUtils = fileUtils;
        _settingsProvider = settingsProvider;
        _repoPath = paths.RepoPath;
    }

    public IEnumerable<string> GetCustomFormatJson()
    {
        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        var exception = CheckoutAndUpdateRepo();
        if (exception is not null)
        {
            _log.Information("Deleting local git repo and retrying git operation...");
            _fileUtils.DeleteReadOnlyDirectory(_repoPath);

            exception = CheckoutAndUpdateRepo();
            if (exception is not null)
            {
                throw exception;
            }
        }

        var jsonDir = Path.Combine(_repoPath, "docs/json/radarr");
        var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
            .Select(async f => await _fileSystem.File.ReadAllTextAsync(f));

        return Task.WhenAll(tasks).Result;
    }

    private Exception? CheckoutAndUpdateRepo()
    {
        var repoSettings = _settingsProvider.Settings.Repository;
        var cloneUrl = repoSettings.CloneUrl;
        const string branch = "master";

        try
        {
            using var repo = _repositoryFactory.CreateAndCloneIfNeeded(cloneUrl, _repoPath, branch);
            repo.ForceCheckout(branch);
            repo.Fetch();
            repo.ResetHard($"origin/{branch}");
        }
        catch (LibGit2SharpException e)
        {
            _log.Error(e, "An exception occurred during git operations on path: {RepoPath}", _repoPath);
            return e;
        }

        return null;
    }
}
