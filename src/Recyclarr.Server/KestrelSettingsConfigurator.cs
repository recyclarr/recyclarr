using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

/// <summary>
/// Configures Kestrel listen options from <see cref="ServerSettings"/>. Resolved from DI after
/// the Autofac container is built, so settings are fully available at configuration time.
/// </summary>
internal sealed class KestrelSettingsConfigurator(ISettings<ServerSettings> serverSettings)
    : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        var settings = serverSettings.Value;
        var address = settings.BindAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback
            : IPAddress.Parse(settings.BindAddress);

        options.Listen(address, settings.Port);
    }
}
