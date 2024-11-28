using Flurl.Http;

namespace Recyclarr.ServarrApi.System;

public class SystemApiService(IServarrRequestBuilder service) : ISystemApiService
{
    public async Task<SystemStatus> GetStatus(CancellationToken ct)
    {
        return await service
            .Request("system", "status")
            .GetJsonAsync<SystemStatus>(cancellationToken: ct);
    }
}
