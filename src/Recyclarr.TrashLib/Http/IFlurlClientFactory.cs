using Flurl.Http;

namespace Recyclarr.TrashLib.Http;

public interface IFlurlClientFactory
{
    IFlurlClient Get(string baseUrl);
}
