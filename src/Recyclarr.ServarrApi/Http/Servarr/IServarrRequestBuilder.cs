using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.Http.Servarr;

public interface IServarrRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
