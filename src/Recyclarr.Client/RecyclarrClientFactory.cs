using System.Diagnostics.CodeAnalysis;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Recyclarr.Client;

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
