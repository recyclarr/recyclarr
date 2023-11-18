using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Http;

public class ServarrRequestBuilder(IFlurlClientCache clientCache) : IServarrRequestBuilder
{
    public IFlurlRequest Request(IServiceConfiguration config, params object[] path)
    {
        var client = clientCache.Get(config.InstanceName);
        client.BaseUrl ??= config.BaseUrl.AppendPathSegments("api", "v3");
        return client.Request(path)
            .WithHeader("X-Api-Key", config.ApiKey);
    }
}
