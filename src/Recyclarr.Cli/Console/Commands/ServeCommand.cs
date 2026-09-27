using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
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

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(serverBinary.FullName) { UseShellExecute = false };
        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}
