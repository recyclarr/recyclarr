using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo.VersionControl;

public interface IGitRepositoryFactory
{
    Task<IGitRepository> CreateAndCloneIfNeeded(
        Uri repoUrl,
        IDirectoryInfo repoPath,
        string branch,
        CancellationToken token);
}
