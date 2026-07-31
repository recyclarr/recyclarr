using Recyclarr.Client.V1;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Resolves which server a command talks to. A configured <c>server.base_url</c> means the user
/// runs their own server (centralized); its absence means one is launched for the duration of the
/// command (ephemeral). See ADR-010.
/// </summary>
internal sealed class ServerConnectionFactory(
    ILogger log,
    ISettings<ServerSettings> settings,
    Func<EphemeralServerLauncher> createLauncher,
    Func<Uri, ISyncApi> createSyncApi
)
{
    public async Task<ServerConnection> ConnectAsync(CancellationToken ct)
    {
        var configuredUrl = settings.Value.BaseUrl;
        if (configuredUrl is not null)
        {
            log.Debug("Using configured server at {BaseUrl}", configuredUrl);
            return new ServerConnection(createSyncApi(configuredUrl), ownedServer: null);
        }

        var launcher = createLauncher();
        var address = await launcher.StartAsync(ct);
        log.Debug("Started ephemeral server at {BaseUrl}", address);
        return new ServerConnection(createSyncApi(address), launcher);
    }
}
