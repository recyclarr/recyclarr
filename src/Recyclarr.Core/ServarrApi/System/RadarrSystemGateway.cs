using Recyclarr.Servarr.SystemStatus;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.System;

internal class RadarrSystemGateway(RadarrApi.ISystemApi api) : ISystemService
{
    public async Task<SystemServiceResult> GetStatus(CancellationToken ct)
    {
        var dto = await api.Status(ct);
        return RadarrSystemMapper.ToDomain(dto);
    }
}
