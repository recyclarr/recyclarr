using Flurl;
using TrashLib.Extensions;

namespace TrashLib.Config.Services;

public class ServerInfo : IServerInfo
{
    private readonly IServiceConfiguration _config;

    public ServerInfo(IServiceConfiguration config)
    {
        _config = config;
    }

    public Url BuildRequest()
    {
        var apiKey = _config.ApiKey;
        var baseUrl = _config.BaseUrl;

        return baseUrl
            .AppendPathSegment("api/v3")
            .SetQueryParams(new {apikey = apiKey});
    }

    public string SanitizedBaseUrl => FlurlLogging.SanitizeUrl(_config.BaseUrl);
}
