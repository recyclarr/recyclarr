using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Server;

/// <summary>
/// A command's connection to a Recyclarr server. Disposal shuts down the server when this
/// connection started one; for a server the user runs themselves, disposal does nothing.
/// </summary>
internal sealed class ServerConnection(ISyncApi sync, IAsyncDisposable? ownedServer)
    : IAsyncDisposable
{
    public ISyncApi Sync { get; } = sync;

    public ValueTask DisposeAsync()
    {
        return ownedServer?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
