using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

// Binds Kestrel to server.bind_address and server.port from settings.yml. An explicit URL
// (--urls or ASPNETCORE_URLS) wins, which is how the ephemeral launcher requests a free loopback
// port. Kestrel endpoints override those URLs, so settings are applied only when none is given.
// The URL form keeps Kestrel's own parsing, so "localhost" and "0.0.0.0" mean what they do in
// --urls.
internal sealed class SettingsEndpointSetup(
    ISettings<ServerSettings> settings,
    IConfiguration configuration
) : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        if (!string.IsNullOrEmpty(configuration[WebHostDefaults.ServerUrlsKey]))
        {
            return;
        }

        var server = settings.Value;
        var endpoints = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Endpoints:Default:Url"] = $"http://{server.BindAddress}:{server.Port}",
                }
            )
            .Build();

        options.Configure(endpoints);
    }
}
