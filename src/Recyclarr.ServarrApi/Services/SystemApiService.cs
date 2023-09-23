using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Dto;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.Services;

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
