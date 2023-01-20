using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Http;

public class ServiceRequestBuilder : IServiceRequestBuilder
{
    private readonly IFlurlClientFactory _clientFactory;

    public ServiceRequestBuilder(IFlurlClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public IFlurlRequest Request(IServiceConfiguration config, params object[] path)
    {
        var client = _clientFactory.BuildClient(config.BaseUrl);
        return client.Request(new[] {"api", "v3"}.Concat(path).ToArray())
            .SetQueryParams(new {apikey = config.ApiKey});
    }
}
