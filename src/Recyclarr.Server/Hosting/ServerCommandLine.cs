using System.CommandLine;
using Serilog.Events;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// The server's command line: launch options only (ADR-037). Directories, the API key, and other
/// settings come from environment variables and settings.yml (ADR-036), so an unknown argument
/// stops startup with a parse error and usage help instead of reaching host configuration.
/// </summary>
internal static class ServerCommandLine
{
    public static Task<int> InvokeAsync(
        string[] args,
        Func<ServerLaunchOptions, Task<int>> runServer
    )
    {
        Option<string> urls = new("--urls")
        {
            Description = "API URLs to listen on, separated by ';'. Overrides ASPNETCORE_URLS.",
        };

        Option<LogEventLevel> logLevel = new("--log-level")
        {
            Description = "Minimum level of log messages.",
            DefaultValueFactory = _ => LogEventLevel.Information,
        };

        // Set by the CLI when it launches the server it owns (ADR-010).
        Option<int?> parentPid = new("--parent-pid") { Hidden = true };
        Option<bool> ephemeral = new("--ephemeral") { Hidden = true };

        RootCommand root =
        [
            with("Recyclarr server: syncs TRaSH Guides to Sonarr and Radarr."),
            urls,
            logLevel,
            parentPid,
            ephemeral,
        ];

        root.SetAction(
            (result, _) =>
                runServer(
                    new ServerLaunchOptions
                    {
                        Urls = result.GetValue(urls),
                        LogLevel = result.GetValue(logLevel),
                        ParentPid = result.GetValue(parentPid),
                        Mode = result.GetValue(ephemeral)
                            ? ServerMode.Ephemeral
                            : ServerMode.Persistent,
                    }
                )
        );

        // The host handles SIGTERM and Ctrl+C itself (systemd, Windows service, console), and
        // shuts down gracefully; System.CommandLine would otherwise force an exit after a timeout.
        // Arguments come only from the command line; an @file token is rejected, not expanded.
        return root.Parse(args, new ParserConfiguration { ResponseFileTokenReplacer = null })
            .InvokeAsync(new InvocationConfiguration { ProcessTerminationTimeout = null });
    }
}
