using System.IO.Abstractions;

namespace Recyclarr.VersionControl;

public interface IGitRepositoryFactory
{
    Task<IGitRepository> CreateAndCloneIfNeeded(
        Uri repoUrl,
        IDirectoryInfo repoPath,
        string reference,
        int depth,
        CancellationToken token
    );
}
