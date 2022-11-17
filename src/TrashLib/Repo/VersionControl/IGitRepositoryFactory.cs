namespace TrashLib.Repo.VersionControl;

public interface IGitRepositoryFactory
{
    Task<IGitRepository> CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch);
}
