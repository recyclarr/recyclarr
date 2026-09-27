namespace Recyclarr.Server.Sync;

// Publishes the startup configuration snapshot. FastEndpoints constructs every endpoint while
// mapping routes, before the host starts, so endpoints cannot take ServerConfiguration as a
// constructor dependency: resolving it would load configuration before resource providers
// initialize. Consumers read Current at request time; ServerBootstrapService loads it first.
internal sealed class ServerConfigurationStore
{
    private ServerConfiguration? _current;

    public ServerConfiguration Current =>
        _current
        ?? throw new InvalidOperationException(
            "Server configuration was read before startup loaded it"
        );

    public void Publish(ServerConfiguration configuration)
    {
        _current = configuration;
    }
}
