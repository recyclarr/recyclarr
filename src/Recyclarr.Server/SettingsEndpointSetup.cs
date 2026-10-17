using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

/// <summary>
/// Binds Kestrel to the API endpoint and, on a persistent server, the management port (ADR-029).
/// </summary>
/// <remarks>
/// The API endpoint comes from an explicit URL (--urls or ASPNETCORE_URLS) when one is given, which
/// is how the ephemeral launcher requests a free loopback port; otherwise from server.bind_address
/// and server.port. Configured Kestrel endpoints replace the URL list entirely, so explicit URLs are
/// re-added as endpoints. The management port binds the same host as the first API endpoint, so
/// the Docker image (all interfaces) and a loopback-only server each keep one exposure rule. The URL
/// form keeps Kestrel's own parsing, so "localhost" and "0.0.0.0" mean what they do in --urls.
/// </remarks>
internal sealed class SettingsEndpointSetup(
    ISettings<ServerSettings> settings,
    IConfiguration configuration,
    ManagementPort managementPort
) : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        var apiUrls = ApiUrls();
        var endpoints = new Dictionary<string, string?>();
        for (var i = 0; i < apiUrls.Count; i++)
        {
            endpoints[$"Endpoints:Api{i}:Url"] = apiUrls[i];
        }

        // A conflicting port is reported by EnsureDistinctPorts, not thrown here: the web host
        // configures Kestrel while it is built, outside the startup failure path.
        if (managementPort.Number is { } port && !UsesApiPort(apiUrls, port))
        {
            var host = BindingAddress.Parse(apiUrls[0]).Host;
            endpoints["Endpoints:Management:Url"] = $"http://{host}:{port}";
        }

        options.Configure(new ConfigurationBuilder().AddInMemoryCollection(endpoints).Build());
    }

    /// <summary>
    /// Fails startup when the management port equals an API port (ADR-029).
    /// </summary>
    public void EnsureDistinctPorts()
    {
        if (managementPort.Number is { } port && UsesApiPort(ApiUrls(), port))
        {
            throw new FatalException(
                $"The management port ({port}) must differ from the API port; "
                    + "change server.management.port in settings.yml"
            );
        }
    }

    private static bool UsesApiPort(List<string> apiUrls, int port)
    {
        return apiUrls.Any(url => BindingAddress.Parse(url).Port == port);
    }

    private List<string> ApiUrls()
    {
        var explicitUrls = configuration[WebHostDefaults.ServerUrlsKey];
        if (!string.IsNullOrEmpty(explicitUrls))
        {
            return explicitUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        var server = settings.Value;
        return [$"http://{server.BindAddress}:{server.Port}"];
    }
}
