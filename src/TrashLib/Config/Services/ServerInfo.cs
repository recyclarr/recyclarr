using Flurl;

namespace TrashLib.Config.Services;

internal class ServerInfo : IServerInfo
{
    private readonly IConfigurationProvider _config;

    public ServerInfo(IConfigurationProvider config)
    {
        _config = config;
    }

    public Url BuildRequest()
    {
        var apiKey = _config.ActiveConfiguration.ApiKey;
        var baseUrl = _config.ActiveConfiguration.BaseUrl;

        return baseUrl
            .AppendPathSegment("api/v3")
            .SetQueryParams(new {apikey = apiKey});
    }
}
