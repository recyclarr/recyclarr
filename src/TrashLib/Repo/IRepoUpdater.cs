namespace TrashLib.Repo;

public interface IRepoUpdater
{
    string RepoPath { get; }
    void UpdateRepo();
}
