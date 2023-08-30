namespace Recyclarr.TrashLib.Repo;

public interface IMultiRepoUpdater
{
    Task UpdateAllRepositories(CancellationToken token);
}
