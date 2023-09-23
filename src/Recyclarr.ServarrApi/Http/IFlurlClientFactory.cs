using Flurl.Http;

namespace Recyclarr.ServarrApi.Http;

public interface IFlurlClientFactory
{
    IFlurlClient BuildClient(Uri baseUrl);
}
