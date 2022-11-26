using Flurl.Http;

namespace TrashLib.Config.Services;

public interface IServiceRequestBuilder
{
    string SanitizedBaseUrl { get; }
    IFlurlRequest Request(params object[] path);
}
