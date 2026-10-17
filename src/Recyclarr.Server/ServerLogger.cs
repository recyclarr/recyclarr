using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Server.Logging;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Telemetry;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Server;

/// <summary>
/// The server's logger, configured in two stages. Until <see cref="Reload"/> it writes only errors
/// to stderr; afterwards it uses the console format and log files from <c>server.logging</c>.
/// </summary>
/// <remarks>
/// Two stages break a dependency cycle: loading settings.yml logs through this logger, so the
/// final configuration cannot exist until settings have loaded. For the same reason the settings
/// are resolved lazily; resolving them while this logger is constructed recurses without end. This
/// is an instance in DI rather than Serilog's static bootstrap logger because tests start many
/// hosts in one process.
/// </remarks>
internal sealed class ServerLogger(
    IAppPaths paths,
    LoggingLevelSwitch levelSwitch,
    ServerMode mode,
    Lazy<ISettings<ServerSettings>> settings,
    IConfiguration configuration
) : ILogger, IDisposable
{
    private volatile Logger _inner = ErrorsToStderr(LogSetup.BaseConfiguration()).CreateLogger();

    public IReadOnlyCollection<IFileInfo> ActiveLogFiles { get; private set; } = [];

    public void Write(LogEvent logEvent)
    {
        _inner.Write(logEvent);
    }

    public void Reload()
    {
        var logging = settings.Value.Value.Logging;
        var config = LogSetup.BaseConfiguration();

        if (logging.Files)
        {
            AddLogFiles(config);
        }

        var format =
            logging.Format
            ?? (Console.IsOutputRedirected ? ServerLogFormat.Json : ServerLogFormat.Text);

        if (mode == ServerMode.Ephemeral)
        {
            // An ephemeral server has no terminal, and its stdout carries only the READY handshake.
            // Errors go to stderr so the launching CLI can explain a failed startup (ADR-019);
            // everything else is in the log files.
            ErrorsToStderr(config);
        }
        else if (format == ServerLogFormat.Json)
        {
            config.WriteTo.Console(new JsonLogFormatter(), levelSwitch: levelSwitch);
        }
        else
        {
            config.WriteTo.Console(
                new ExpressionTemplate(
                    "[{@l:u3}] " + LogSetup.BaseTemplate,
                    theme: TemplateTheme.Code
                ),
                levelSwitch: levelSwitch
            );
        }

        // The sink reads the standard OTEL_EXPORTER_OTLP_* variables itself; they override the
        // resource attributes given here. It follows the console level: below it, framework
        // events fire for every request, including each health probe.
        if (ServerTelemetry.ExportsOtlp(configuration))
        {
            config.WriteTo.Logger(c =>
                c.MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.OpenTelemetry(o =>
                        o.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = ServerTelemetry.ServiceName(configuration),
                            ["service.version"] = ServerTelemetry.ServiceVersion,
                        }
                    )
            );
        }

        var previous = _inner;
        _inner = config.CreateLogger();
        previous.Dispose();
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    private void AddLogFiles(LoggerConfiguration config)
    {
        var prefix = $"recyclarr-server_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var debugLogFile = paths.ServerLogDirectory.File($"{prefix}.debug.log");
        var verboseLogFile = paths.ServerLogDirectory.File($"{prefix}.verbose.log");
        ActiveLogFiles = [debugLogFile, verboseLogFile];
        var template = new ExpressionTemplate(LogSetup.FileTemplate);

        config
            .WriteTo.Logger(c =>
                c.MinimumLevel.Debug().WriteTo.File(template, debugLogFile.FullName)
            )
            .WriteTo.Logger(c =>
                c.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                    .WriteTo.File(template, verboseLogFile.FullName)
            );
    }

    private static LoggerConfiguration ErrorsToStderr(LoggerConfiguration config)
    {
        return config.WriteTo.Console(
            new ExpressionTemplate(LogSetup.BaseTemplate),
            restrictedToMinimumLevel: LogEventLevel.Error,
            standardErrorFromLevel: LogEventLevel.Error
        );
    }
}
