using Flurl.Http;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Http;

public interface IServiceRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
