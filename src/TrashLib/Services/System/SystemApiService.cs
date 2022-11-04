using Flurl;
using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Services.System.Dto;

namespace TrashLib.Services.System;

public class SystemApiService : ISystemApiService
{
    private readonly IServerInfo _serverInfo;

    public SystemApiService(IServerInfo serverInfo)
    {
        _serverInfo = serverInfo;
    }

    public async Task<SystemStatus> GetStatus()
    {
        return await BaseUrl()
            .AppendPathSegment("system/status")
            .GetJsonAsync<SystemStatus>();
    }

    private Url BaseUrl() => _serverInfo.BuildRequest();
}
