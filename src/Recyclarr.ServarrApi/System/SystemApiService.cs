using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.System;

public class SystemApiService(IServarrRequestBuilder service) : ISystemApiService
{
    public async Task<SystemStatus> GetStatus(IServiceConfiguration config)
    {
        return await service.Request(config, "system", "status")
            .GetJsonAsync<SystemStatus>();
    }
}
