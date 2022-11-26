using Flurl.Http;

namespace TrashLib.Http;

public interface IFlurlClientFactory
{
    IFlurlClient Get(string baseUrl);
}
