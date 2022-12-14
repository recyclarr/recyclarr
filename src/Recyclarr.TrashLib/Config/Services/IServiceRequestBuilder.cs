using Flurl.Http;

namespace Recyclarr.TrashLib.Config.Services;

public interface IServiceRequestBuilder
{
    string SanitizedBaseUrl { get; }
    IFlurlRequest Request(params object[] path);
}
