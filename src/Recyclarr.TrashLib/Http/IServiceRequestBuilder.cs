using Flurl.Http;

namespace Recyclarr.TrashLib.Http;

public interface IServiceRequestBuilder
{
    string SanitizedBaseUrl { get; }
    IFlurlRequest Request(params object[] path);
}
