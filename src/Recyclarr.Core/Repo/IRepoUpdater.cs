namespace Recyclarr.Repo;

public interface IRepoUpdater
{
    Task UpdateRepo(GitRepositorySource repositorySource, CancellationToken token);
}
