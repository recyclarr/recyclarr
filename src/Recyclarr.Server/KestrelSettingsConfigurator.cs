using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

/// <summary>
/// Configures Kestrel listen options from <see cref="ServerSettings"/>, with CLI args taking
/// precedence over YAML settings.
/// </summary>
internal sealed class KestrelSettingsConfigurator(
    ISettings<ServerSettings> serverSettings,
    ServerArgsParser.ParsedArgs cliArgs
) : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        var settings = serverSettings.Value;
        var port = cliArgs.Port ?? settings.Port;
        var bindAddressStr = cliArgs.BindAddress ?? settings.BindAddress;

        var address = bindAddressStr.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback
            : IPAddress.Parse(bindAddressStr);

        options.Listen(address, port);
    }
}
