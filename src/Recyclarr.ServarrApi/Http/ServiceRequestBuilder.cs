using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Http;

public class ServiceRequestBuilder : IServiceRequestBuilder
{
    private readonly IFlurlClientFactory _clientFactory;

    public ServiceRequestBuilder(IFlurlClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public IFlurlRequest Request(IServiceConfiguration config, params object[] path)
    {
        var client = _clientFactory.Get(config.BaseUrl);
        return client.Request(new[] {"api", "v3"}.Concat(path).ToArray())
            .WithHeader("X-Api-Key", config.ApiKey);
    }
}
