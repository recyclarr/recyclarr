namespace Recyclarr.TrashLib.Repo.VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly Func<string, IGitRepository> _repoFactory;
    private readonly ILogger _log;

    public GitRepositoryFactory(
        Func<string, IGitRepository> repoFactory,
        ILogger log)
    {
        _repoFactory = repoFactory;
        _log = log;
    }

    public async Task<IGitRepository> CreateAndCloneIfNeeded(Uri repoUrl, string repoPath, string branch)
    {
        var repo = _repoFactory(repoPath);

        if (!repo.Path.Exists)
        {
            _log.Information("Cloning trash repository...");
            await repo.Clone(repoUrl, branch);
        }
        else
        {
            // Run just to check repository health. If unhealthy, an exception will
            // be thrown. That exception will propagate up and result in a re-clone.
            await repo.Status();
        }

        _log.Debug("Remote 'origin' set to {Url}", repoUrl);
        await repo.SetRemote("origin", repoUrl);
        return repo;
    }
}
