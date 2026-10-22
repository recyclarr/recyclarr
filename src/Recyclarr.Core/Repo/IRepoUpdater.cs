namespace Recyclarr.Repo;

public enum RepoUpdateOutcome
{
    Fetched,

    // Every reference failed to fetch; the working tree holds the previous checkout.
    FetchFailedUsingCache,
}

public interface IRepoUpdater
{
    Task<RepoUpdateOutcome> UpdateRepo(
        GitRepositorySource repositorySource,
        CancellationToken token
    );
}
