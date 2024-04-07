using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi;

public interface IServarrRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
