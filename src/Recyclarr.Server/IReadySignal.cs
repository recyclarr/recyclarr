namespace Recyclarr.Server;

/// <summary>
/// Signals that the server has finished starting and is ready to accept requests. The default
/// implementation writes the READY handshake line consumed by the ephemeral-launch parent process;
/// tests substitute a no-op adapter since there's no parent process reading stdout.
/// </summary>
internal interface IReadySignal
{
    void Ready(int port);
}

internal sealed class ConsoleReadySignal : IReadySignal
{
    public void Ready(int port) => Console.WriteLine($"READY:{port}");
}
