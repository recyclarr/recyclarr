using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.System;

public class SystemApiService : ISystemApiService
{
    private readonly IServarrRequestBuilder _service;

    public SystemApiService(IServarrRequestBuilder service)
    {
        _service = service;
    }

    public async Task<SystemStatus> GetStatus(IServiceConfiguration config)
    {
        return await _service.Request(config, "system", "status")
            .GetJsonAsync<SystemStatus>();
    }
}
