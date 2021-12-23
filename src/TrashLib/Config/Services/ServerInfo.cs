using Flurl;
using Flurl.Http;
using Serilog;
using TrashLib.Extensions;

namespace TrashLib.Config.Services;

internal class ServerInfo : IServerInfo
{
    private readonly IConfigurationProvider _config;
    private readonly ILogger _log;

    public ServerInfo(IConfigurationProvider config, ILogger log)
    {
        _config = config;
        _log = log;
    }

    public IFlurlRequest BuildRequest()
    {
        var apiKey = _config.ActiveConfiguration.ApiKey;
        var baseUrl = _config.ActiveConfiguration.BaseUrl;

        return baseUrl
            .AppendPathSegment("api/v3")
            .SetQueryParams(new {apikey = apiKey})
            .SanitizedLogging(_log);
    }
}
