using Flurl.Http;

namespace Recyclarr.TrashLib.Http;

public interface IServiceRequestBuilder
{
    IFlurlRequest Request(params object[] path);
}
