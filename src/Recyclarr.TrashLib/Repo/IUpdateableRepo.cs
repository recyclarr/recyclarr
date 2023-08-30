namespace Recyclarr.TrashLib.Repo;

public interface IUpdateableRepo
{
    Task Update(CancellationToken token);
}
