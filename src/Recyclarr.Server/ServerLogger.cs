using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Server.Persistence;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

namespace Recyclarr.Server;

/// <summary>
/// Configures logging for the server host after its services are available.
/// </summary>
internal sealed class ServerLogger(IAppPaths paths, LoggingLevelSwitch levelSwitch, ServerMode mode)
{
    public IReadOnlyCollection<IFileInfo> ActiveLogFiles { get; private set; } = [];

    public void Configure(LoggerConfiguration config)
    {
        var prefix = $"recyclarr-server_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var debugLogFile = paths.ServerLogDirectory.File($"{prefix}.debug.log");
        var verboseLogFile = paths.ServerLogDirectory.File($"{prefix}.verbose.log");
        ActiveLogFiles = [debugLogFile, verboseLogFile];
        var template = new ExpressionTemplate(LogSetup.FileTemplate);

        config
            .MinimumLevel.Verbose()
            .MinimumLevel.Override(
                "Microsoft.AspNetCore.Hosting.Diagnostics",
                LogEventLevel.Warning
            )
            .Enrich.FromLogContext()
            .Enrich.With<ExceptionSanitizingEnricher>()
            .WriteTo.Logger(c =>
                c.MinimumLevel.Debug().WriteTo.File(template, debugLogFile.FullName)
            )
            .WriteTo.Logger(c =>
                c.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                    .WriteTo.File(template, verboseLogFile.FullName)
            );

        if (mode == ServerMode.Ephemeral)
        {
            // An ephemeral server has no terminal, and its stdout carries only the READY handshake.
            // Errors go to stderr so the launching CLI can explain a failed startup (ADR-019);
            // everything else is in the log files.
            config.WriteTo.Console(
                new ExpressionTemplate(LogSetup.BaseTemplate),
                restrictedToMinimumLevel: LogEventLevel.Error,
                standardErrorFromLevel: LogEventLevel.Error
            );
            return;
        }

        config.WriteTo.Console(
            new ExpressionTemplate("[{@l:u3}] " + LogSetup.BaseTemplate, theme: TemplateTheme.Code),
            levelSwitch: levelSwitch
        );
    }
}
