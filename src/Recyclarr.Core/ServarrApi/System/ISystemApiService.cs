namespace Recyclarr.ServarrApi.System;

public interface ISystemApiService
{
    Task<ServiceSystemStatus> GetStatus(CancellationToken ct);
}
