using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Http;

public interface IServiceRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
