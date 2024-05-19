using Flurl.Http;

namespace Recyclarr.ServarrApi;

public interface IServarrRequestBuilder
{
    IFlurlRequest Request(params object[] path);
}
