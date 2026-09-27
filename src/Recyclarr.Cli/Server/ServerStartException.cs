namespace Recyclarr.Cli.Server;

// The ephemeral server exited before it was ready. Errors holds the error log lines the server
// wrote while starting, which explain why (for example, invalid configuration).
internal sealed class ServerStartException(IReadOnlyList<string> errors)
    : Exception("The Recyclarr server failed to start")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
