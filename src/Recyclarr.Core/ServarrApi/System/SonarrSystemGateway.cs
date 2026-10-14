using Recyclarr.Servarr.SystemStatus;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.System;

internal class SonarrSystemGateway(SonarrApi.ISystemApi api) : ISystemService
{
    public async Task<SystemServiceResult> GetStatus(CancellationToken ct)
    {
        var dto = await api.Status(ct);
        return SonarrSystemMapper.ToDomain(dto);
    }
}
