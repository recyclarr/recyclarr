using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.System.Dto;

namespace Recyclarr.TrashLib.Services.System;

public class SystemApiService : ISystemApiService
{
    private readonly IServiceRequestBuilder _service;

    public SystemApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<SystemStatus> GetStatus(IServiceConfiguration config)
    {
        return await _service.Request(config, "system", "status")
            .GetJsonAsync<SystemStatus>();
    }
}
