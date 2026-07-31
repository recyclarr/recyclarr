using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

namespace Recyclarr.Server;

/// <summary>
/// Configures logging for the server host after its services are available.
/// </summary>
internal sealed class ServerLogger(
    IAppPaths paths,
    LoggingLevelSwitch levelSwitch,
    ServerLogOptions options
)
{
    public void Configure(LoggerConfiguration config)
    {
        var prefix = $"recyclarr-server_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var logFile = paths.LogDirectory.File($"{prefix}.debug.log");
        var template = new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " + LogSetup.BaseTemplate + "{Inspect(@x).StackTrace}"
        );

        config
            .MinimumLevel.Verbose()
            .MinimumLevel.Override(
                "Microsoft.AspNetCore.Hosting.Diagnostics",
                LogEventLevel.Warning
            )
            .Enrich.FromLogContext()
            .Enrich.With<ExceptionSanitizingEnricher>()
            .WriteTo.Logger(c => c.MinimumLevel.Debug().WriteTo.File(template, logFile.FullName));

        // Ephemeral mode uses one event per line so the parent can re-emit it at the right level.
        // Standalone mode renders the same console format as the CLI logger.
        var consoleTemplate = options.UseParentProtocol
            ? $"{ServerLogOptions.StdoutPrefix}{{@l}}:" + LogSetup.BaseTemplate
            : "[{@l:u3}] " + LogSetup.BaseTemplate;
        var consoleTheme = options.UseParentProtocol ? null : TemplateTheme.Code;
        config.WriteTo.Console(
            new ExpressionTemplate(consoleTemplate, theme: consoleTheme),
            levelSwitch: levelSwitch
        );
    }
}
