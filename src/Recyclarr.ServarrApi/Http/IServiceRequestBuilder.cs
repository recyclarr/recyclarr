using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Http;

public interface IServiceRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
