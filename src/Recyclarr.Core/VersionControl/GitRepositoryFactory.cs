using System.IO.Abstractions;

namespace Recyclarr.VersionControl;

public class GitRepositoryFactory(ILogger log, IGitPath gitPath) : IGitRepositoryFactory
{
    private static readonly string[] ValidGitPaths = [".git/config", ".git/index", ".git/HEAD"];

    public async Task<IGitRepository> CreateAndCloneIfNeeded(
        Uri repoUrl,
        IDirectoryInfo repoPath,
        string reference,
        int depth,
        CancellationToken token
    )
    {
        var repo = new GitRepository(log, gitPath, repoPath);

        if (!repoPath.Exists)
        {
            log.Debug(
                "Cloning repository to {RepoPath} with depth {Depth}",
                repoPath.FullName,
                depth
            );
            await repo.Clone(token, repoUrl, reference, depth);
        }
        else
        {
            // Check if the `.git` directory is present and intact. We used to just do a `git status` here, but
            // this sometimes has a false positive if our repo directory is inside another repository.
            if (ValidGitPaths.Select(repoPath.File).Any(x => !x.Exists))
            {
                throw new InvalidGitRepoException("A `.git` directory or its files are missing");
            }

            // Run just to check repository health. If unhealthy, an exception will
            // be thrown. That exception will propagate up and result in a re-clone.
            await repo.Status(token);
        }

        return repo;
    }
}
