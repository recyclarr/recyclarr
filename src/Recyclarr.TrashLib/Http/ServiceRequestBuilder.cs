using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Http;

public class ServiceRequestBuilder : IServiceRequestBuilder
{
    private readonly IServiceConfiguration _config;
    private readonly IFlurlClientFactory _clientFactory;

    public ServiceRequestBuilder(IServiceConfiguration config, IFlurlClientFactory clientFactory)
    {
        _config = config;
        _clientFactory = clientFactory;
    }

    public IFlurlRequest Request(params object[] path)
    {
        var client = _clientFactory.Get(_config.BaseUrl);
        return client.Request(new[] {"api", "v3"}.Concat(path).ToArray())
            .SetQueryParams(new {apikey = _config.ApiKey});
    }

    public string SanitizedBaseUrl => FlurlLogging.SanitizeUrl(_config.BaseUrl);
}
