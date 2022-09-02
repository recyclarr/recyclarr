using Flurl;
using TrashLib.Extensions;

namespace TrashLib.Config.Services;

public class ServerInfo : IServerInfo
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

    public string SanitizedBaseUrl => FlurlLogging.SanitizeUrl(_config.ActiveConfiguration.BaseUrl);
}
