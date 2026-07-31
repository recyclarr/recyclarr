namespace Recyclarr.Server.TestLibrary;

// Test adapter for the READY stdout handshake port. There's no parent process reading stdout
// under WebApplicationFactory, so this is a no-op rather than a raw Console.WriteLine.
internal sealed class NoOpReadySignal : IReadySignal
{
    public void Ready(int port) { }
}
