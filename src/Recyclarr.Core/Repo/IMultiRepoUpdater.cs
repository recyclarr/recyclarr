namespace Recyclarr.Repo;

public interface IMultiRepoUpdater
{
    Task UpdateAllRepositories(bool hideConsoleOutput, CancellationToken token);
}
