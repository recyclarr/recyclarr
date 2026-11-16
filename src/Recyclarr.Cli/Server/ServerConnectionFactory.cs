using System.Net.Http.Headers;
using System.Security.Cryptography;
using Recyclarr.Cli.Settings;

namespace Recyclarr.Cli.Server;

/// <summary>
/// Resolves which server a command talks to. A configured server address (see
/// <see cref="CliSettingsLoader"/>) means the user runs their own server (centralized); its
/// absence means one is launched for the duration of the command (ephemeral). See ADR-010.
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
        var settings = settingsLoader.Load();
        if (settings.ServerBaseUrl is { } configuredUrl)
        {
            log.Debug("Using configured server at {BaseUrl}", configuredUrl);
            var client = CreateClient(configuredUrl, settings.ServerApiKey);
            return new ServerConnection(client, ownedServer: null);
        }

        // A one-time key keeps the private server closed to other local processes (ADR-027).
        var apiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var launcher = createLauncher();
        var address = await launcher.StartAsync(apiKey, ct);
        log.Debug("Started ephemeral server at {BaseUrl}", address);
        return new ServerConnection(CreateClient(address, apiKey), launcher);
    }

    /// <summary>
    /// Without a key the request carries no Authorization header, and the server answers 401.
    /// </summary>
    private HttpClient CreateClient(Uri baseAddress, string? apiKey)
    {
        var client = createClient();
        client.BaseAddress = baseAddress;
        if (apiKey is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                apiKey
            );
        }

        return client;
    }
}
