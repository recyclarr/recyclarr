using Recyclarr.Servarr.SystemStatus;

namespace Recyclarr.ServarrApi.System;

internal class RadarrSystemGateway(ISystemApiService api) : ISystemService
{
    public async Task<SystemServiceResult> GetStatus(CancellationToken ct)
    {
        var dto = await api.GetStatus(ct);
        return ToDomain(dto);
    }

    private static SystemServiceResult ToDomain(ServiceSystemStatus dto)
    {
        return new SystemServiceResult(dto.AppName, new Version(dto.Version));
    }
}
