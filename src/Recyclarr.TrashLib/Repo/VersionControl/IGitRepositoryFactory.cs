namespace Recyclarr.TrashLib.Repo.VersionControl;

public interface IGitRepositoryFactory
{
    Task<IGitRepository> CreateAndCloneIfNeeded(Uri repoUrl, string repoPath, string branch);
}
