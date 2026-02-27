using Recyclarr.Compatibility;

namespace Recyclarr.ServarrApi.System;

internal class SonarrSystemAdapter(ISystemApiService api) : ISystemService
{
    public async Task<SystemServiceResult> GetStatus(CancellationToken ct)
    {
        var dto = await api.GetStatus(ct);
        return ToDomain(dto);
    }

    private static SystemServiceResult ToDomain(SystemStatus dto)
    {
        return new SystemServiceResult(dto.AppName, new Version(dto.Version));
    }
}
