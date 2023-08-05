using System.Diagnostics.CodeAnalysis;
using Flurl.Http.Configuration;

namespace Recyclarr.Common.Networking;

[SuppressMessage("SonarCloud", "S4830:Server certificates should be verified during SSL/TLS connections")]
public class UntrustedCertClientFactory : DefaultHttpClientFactory
{
    public override HttpMessageHandler CreateMessageHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
    }
}
