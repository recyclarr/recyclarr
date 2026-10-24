using Recyclarr.Server.Hosting;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Observability;

/// <summary>
/// The port that alone serves health probes and metrics (ADR-029). Only a persistent server opens
/// one. The host setting <c>managementport</c> (<c>ASPNETCORE_MANAGEMENTPORT</c>), which the Docker
/// image sets, wins over <c>server.management.port</c>.
/// </summary>
internal sealed class ManagementPort(
    ISettings<ServerSettings> settings,
    IConfiguration configuration,
    ServerMode mode
)
{
    public const string ConfigurationKey = "managementport";

    /// <summary>
    /// The port number, or null when this server opens no management port.
    /// </summary>
    public int? Number =>
        mode == ServerMode.Ephemeral
            ? null
            : configuration.GetValue<int?>(ConfigurationKey) ?? settings.Value.Management.Port;

    public bool ServesMetrics => settings.Value.Management.Metrics;
}
