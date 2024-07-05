namespace Recyclarr.ServarrApi.System;

public interface ISystemApiService
{
    Task<SystemStatus> GetStatus(CancellationToken ct);
}
