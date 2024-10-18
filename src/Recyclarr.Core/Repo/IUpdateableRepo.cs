namespace Recyclarr.Repo;

public interface IUpdateableRepo
{
    Task Update(CancellationToken token);
}
