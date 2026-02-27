namespace Recyclarr.Servarr.SystemStatus;

public interface ISystemService
{
    Task<SystemServiceResult> GetStatus(CancellationToken ct);
}
