using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Http;

public interface IServarrRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
