using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Http;

public interface IServiceRequestBuilder
{
    IFlurlRequest Request(IServiceConfiguration config, params object[] path);
}
