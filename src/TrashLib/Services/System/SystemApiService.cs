using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Services.System.Dto;

namespace TrashLib.Services.System;

public class SystemApiService : ISystemApiService
{
    private readonly IServiceRequestBuilder _service;

    public SystemApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<SystemStatus> GetStatus()
    {
        return await _service.Request("system", "status")
            .GetJsonAsync<SystemStatus>();
    }
}
