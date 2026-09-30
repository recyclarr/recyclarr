using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Recyclarr.Cli.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

/// <summary>
/// Starts the server in the foreground. The server reads its own bind address and port
/// (settings.yml, or --urls/ASPNETCORE_URLS), so this command passes nothing to it.
/// </summary>
[Description("Run the Recyclarr HTTP server in the foreground")]
[UsedImplicitly]
internal class ServeCommand(ILogger log, IAnsiConsole console, IFileSystem fs)
    : AsyncCommand<ServeCommand.Settings>
{
    private int _interrupted;

    [UsedImplicitly]
    internal class Settings : BaseCommandSettings;

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken ct
    )
    {
        var serverBinary = ServerBinaryLocator.GetServerBinary(fs);

        if (!serverBinary.Exists)
        {
            log.Error("Server binary not found at {Path}", serverBinary.FullName);
            console.MarkupLineInterpolated(
                $"[red]Error:[/] Server binary not found: {serverBinary.FullName}"
            );
            return (int)ExitStatus.Failed;
        }

        log.Debug("Starting server process: {Path}", serverBinary.FullName);

        // Registered before the server starts, so no Ctrl+C can end the CLI and leave it behind
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnInterrupt);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(serverBinary.FullName) { UseShellExecute = false };
        process.Start();

        // Deliberately ignores ct: the server owns shutdown, and its exit code is the result
        await process.WaitForExitAsync(CancellationToken.None);
        log.Debug("Server process exited with code {ExitCode}", process.ExitCode);
        return process.ExitCode;
    }

    /// <summary>
    /// Ctrl+C reaches the server directly because it shares the terminal's process group, and
    /// the server shuts down gracefully on it. The first interrupt is therefore swallowed so the
    /// CLI outlives the server and reports its exit code. A repeated interrupt is not swallowed,
    /// so a server that hangs on shutdown cannot trap the user.
    /// </summary>
    private void OnInterrupt(PosixSignalContext context)
    {
        if (Interlocked.Exchange(ref _interrupted, 1) == 0)
        {
            context.Cancel = true;
        }
    }
}
