namespace VersionControl;

public interface IGitRepositoryFactory
{
    IGitRepository CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch);
}
