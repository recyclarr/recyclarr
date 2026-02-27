namespace Recyclarr.Compatibility;

public interface ISystemService
{
    Task<SystemServiceResult> GetStatus(CancellationToken ct);
}
