using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Run the Recyclarr HTTP server in the foreground")]
[UsedImplicitly]
internal class ServeCommand(ILogger log, IAnsiConsole console, IFileSystem fs)
    : AsyncCommand<ServeCommand.Settings>
{
    [UsedImplicitly]
    internal class Settings : BaseCommandSettings { }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken ct
    )
    {
        var serverBinary = GetServerBinary();

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

    private IFileInfo GetServerBinary()
    {
        // non-null: ProcessPath is only null in bundled single-file hosts without apphost
        var processDir = fs.FileInfo.New(Environment.ProcessPath!).Directory!;
        var name = OperatingSystem.IsWindows() ? "recyclarr-server.exe" : "recyclarr-server";
        return processDir.File(name);
    }
}
