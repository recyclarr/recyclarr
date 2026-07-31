using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// Fails any attempt to make a real HTTP request. Integration tests stub outbound calls at the
/// Refit interface; this exists so a test that reaches a Refit client nobody stubbed fails
/// immediately and says so, instead of connecting to whatever is listening on the developer's
/// machine or hanging until the connect timeout expires.
/// </summary>
public sealed class BlockedHttpClientFactory : IHttpClientFactory
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "HttpClient owns and disposes the handler"
    )]
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new BlockedHttpMessageHandler(name), disposeHandler: true);
    }
}

internal sealed class BlockedHttpMessageHandler(string clientName) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        throw new InvalidOperationException(
            $"Test attempted real HTTP egress on the '{clientName}' client: "
                + $"{request.Method} {request.RequestUri}. Stub the Refit interface for this call."
        );
    }
}
