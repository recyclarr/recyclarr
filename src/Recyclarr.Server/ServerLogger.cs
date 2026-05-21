using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Serilog.Events;
using Serilog.Templates;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;
using SerilogILogger = Serilog.ILogger;
using SerilogLogger = Serilog.Core.Logger;

namespace Recyclarr.Server;

/// <summary>
/// Logger for the server binary with lazy file-sink initialization. Registered as a singleton;
/// the inner Serilog logger is built on first write after IAppPaths resolves the log directory.
/// </summary>
internal sealed class ServerLogger(IAppPaths paths, LoggingLevelSwitch levelSwitch) : SerilogILogger
{
    private readonly Lazy<SerilogLogger> _inner = new(() => BuildLogger(paths, levelSwitch));

    public void Write(LogEvent logEvent) => _inner.Value.Write(logEvent);

    private static SerilogLogger BuildLogger(IAppPaths paths, LoggingLevelSwitch levelSwitch)
    {
        var prefix = $"recyclarr-server_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var logFile = paths.LogDirectory.File($"{prefix}.debug.log");
        var template = new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " + LogSetup.BaseTemplate + "{Inspect(@x).StackTrace}"
        );

        return LogSetup
            .BaseConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Logger(c => c.MinimumLevel.Debug().WriteTo.File(template, logFile.FullName))
            .CreateLogger();
    }
}
