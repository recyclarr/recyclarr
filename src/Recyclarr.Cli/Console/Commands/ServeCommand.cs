using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Run the Recyclarr HTTP server in the foreground")]
[UsedImplicitly]
internal class ServeCommand(
    ILogger log,
    IAnsiConsole console,
    IFileSystem fs,
    ISettings<ServerSettings> serverSettings
) : AsyncCommand<ServeCommand.Settings>
{
    [UsedImplicitly]
    internal class Settings : BaseCommandSettings
    {
        [CommandOption("--port")]
        [Description("Port to listen on (overrides settings.yml)")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public int? Port { get; init; }

        [CommandOption("--bind-address")]
        [Description("Address to bind to (overrides settings.yml)")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? BindAddress { get; init; }
    }

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

        var urls = ServerUrlBuilder.Build(
            serverSettings.Value,
            settings.Port,
            settings.BindAddress
        );

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(serverBinary.FullName)
        {
            UseShellExecute = false,
            Arguments = $"--urls={urls}",
        };
        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}
