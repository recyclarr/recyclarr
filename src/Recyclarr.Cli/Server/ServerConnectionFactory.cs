using Recyclarr.Cli.Settings;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Resolves which server a command talks to. A <c>server.base_url</c> in <c>cli.yml</c> means the
/// user runs their own server (centralized); its absence means one is launched for the duration
/// of the command (ephemeral). See ADR-010.
/// </summary>
internal sealed class ServerConnectionFactory(
    ILogger log,
    CliSettingsLoader settingsLoader,
    Func<EphemeralServerLauncher> createLauncher,
    Func<HttpClient> createClient
)
{
    public async Task<ServerConnection> ConnectAsync(CancellationToken ct)
    {
        var configuredUrl = settingsLoader.Load().ServerBaseUrl;
        if (configuredUrl is not null)
        {
            log.Debug("Using configured server at {BaseUrl}", configuredUrl);
            return new ServerConnection(CreateClient(configuredUrl), ownedServer: null);
        }

        var launcher = createLauncher();
        var address = await launcher.StartAsync(ct);
        log.Debug("Started ephemeral server at {BaseUrl}", address);
        return new ServerConnection(CreateClient(address), launcher);
    }

    private HttpClient CreateClient(Uri baseAddress)
    {
        var client = createClient();
        client.BaseAddress = baseAddress;
        return client;
    }
}
