using Serilog.Events;

namespace Recyclarr.Server;

/// <summary>
/// How this server process reports its own logs, decided by the command line at startup.
/// </summary>
/// <param name="MinimumLevel">Level for the log file and stdout sinks.</param>
/// <param name="UseParentProtocol">
/// Set when launched by a parent CLI process (ephemeral mode) to use its stdout line protocol.
/// Standalone mode writes normal console output instead.
/// </param>
internal sealed record ServerLogOptions(LogEventLevel MinimumLevel, bool UseParentProtocol)
{
    public const string StdoutPrefix = "LOG:";
}
