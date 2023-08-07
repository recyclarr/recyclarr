using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo.VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly ILogger _log;
    private readonly IGitPath _gitPath;

    public GitRepositoryFactory(ILogger log, IGitPath gitPath)
    {
        _log = log;
        _gitPath = gitPath;
    }

    public async Task<IGitRepository> CreateAndCloneIfNeeded(Uri repoUrl, IDirectoryInfo repoPath, string branch)
    {
        var repo = new GitRepository(_log, _gitPath, repoPath);

        if (!repoPath.Exists)
        {
            _log.Information("Cloning '{RepoName}' repository...", repoPath.Name);
            await repo.Clone(repoUrl, branch, 1);
        }
        else
        {
            // Run just to check repository health. If unhealthy, an exception will
            // be thrown. That exception will propagate up and result in a re-clone.
            await repo.Status();
        }

        await repo.SetRemote("origin", repoUrl);
        _log.Debug("Remote 'origin' set to {Url}", repoUrl);

        return repo;
    }
}
