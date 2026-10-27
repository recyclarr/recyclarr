using Recyclarr.Server.Hosting;

namespace Recyclarr.Server.TestLibrary;

// Test adapter for the READY stdout handshake port. There's no parent process reading stdout
// for an in-process test server, so this is a no-op rather than a raw Console.WriteLine.
internal sealed class NoOpReadySignal : IReadySignal
{
    public void Ready(int port) { }
}
