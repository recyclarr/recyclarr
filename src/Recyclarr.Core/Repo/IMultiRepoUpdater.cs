namespace Recyclarr.Repo;

public interface IMultiRepoUpdater
{
    Task UpdateAllRepositories(CancellationToken token, bool hideConsoleOutput = false);
}
