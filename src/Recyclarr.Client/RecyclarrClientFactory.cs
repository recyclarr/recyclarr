using System.Diagnostics.CodeAnalysis;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Recyclarr.Client;

/// <summary>
/// Creates <see cref="RecyclarrApiClient"/> instances configured with the server's base address.
/// All HTTP communication with the Recyclarr server flows through the generated client; no raw
/// <c>HttpClient</c> usage.
/// </summary>
[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Adapter lifetime is tied to the client; both live for the CLI command duration"
)]
[SuppressMessage(
    "Performance",
    "CA1822",
    Justification = "Resolved through DI as an instance; static would prevent container resolution"
)]
public class RecyclarrClientFactory
{
    /// <summary>
    /// Creates a new <see cref="RecyclarrApiClient"/> targeting the given base address.
    /// </summary>
    public RecyclarrApiClient Create(string baseAddress)
    {
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider)
        {
            BaseUrl = baseAddress.TrimEnd('/'),
        };
        return new RecyclarrApiClient(adapter);
    }
}
