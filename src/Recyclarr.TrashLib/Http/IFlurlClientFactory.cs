using Flurl.Http;

namespace Recyclarr.TrashLib.Http;

public interface IFlurlClientFactory
{
    IFlurlClient BuildClient(Uri baseUrl);
}
