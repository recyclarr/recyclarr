using Microsoft.Extensions.Options;
using Recyclarr.Server.Observability;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// The URLs the server listens on: the API endpoint and, on a persistent server, the management
/// port (ADR-029).
/// </summary>
/// <remarks>
/// The API endpoint comes from an explicit URL (--urls or ASPNETCORE_URLS) when one is given, which
/// is how the ephemeral launcher requests a free loopback port; otherwise from server.bind_address
/// and server.port. The management port binds the same host as the first API endpoint, so the
/// Docker image (all interfaces) and a loopback-only server each keep one exposure rule. The URLs
/// fill the server address list before start; the host reads --urls only when that list is
/// empty, so Kestrel sees one address source. Configured Kestrel endpoints would instead replace
/// the URL list and log a warning that it is ignored.
/// </remarks>
internal sealed class ListenUrls(
    IOptions<ServerSettings> settings,
    IConfiguration configuration,
    ManagementPort managementPort
)
{
    /// <summary>
    /// Call <see cref="EnsureValidPorts"/> first; this does not validate the management port.
    /// </summary>
    public IReadOnlyList<string> Resolve()
    {
        var urls = ApiUrls();
        if (managementPort.Number is { } port && !UsesApiPort(urls, port))
        {
            var host = BindingAddress.Parse(urls[0]).Host;
            urls.Add($"http://{host}:{port}");
        }

        return urls;
    }

    /// <summary>
    /// Fails startup when server.port is out of range (unless an explicit URL replaces it), or when
    /// the management port is out of range or equals an API port (ADR-029). Port 0 is rejected
    /// too: clients and probes need a known port.
    /// </summary>
    public void EnsureValidPorts()
    {
        if (string.IsNullOrEmpty(ExplicitUrls()) && settings.Value.Port is < 1 or > 65535)
        {
            throw new FatalException(
                $"The API port ({settings.Value.Port}) must be between 1 and 65535; "
                    + "change server.port in settings.yml"
            );
        }

        if (managementPort.Number is not { } port)
        {
            return;
        }

        if (port is < 1 or > 65535)
        {
            throw new FatalException(
                $"The management port ({port}) must be between 1 and 65535; "
                    + "change server.management.port in settings.yml"
            );
        }

        if (UsesApiPort(ApiUrls(), port))
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

    private string? ExplicitUrls() => configuration[WebHostDefaults.ServerUrlsKey];

    private List<string> ApiUrls()
    {
        var explicitUrls = ExplicitUrls();
        if (!string.IsNullOrEmpty(explicitUrls))
        {
            return explicitUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        var server = settings.Value;
        return [$"http://{server.BindAddress}:{server.Port}"];
    }
}
