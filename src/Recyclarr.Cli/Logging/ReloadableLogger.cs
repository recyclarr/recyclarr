using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Cli.Logging;

/// <summary>
/// Logger wrapper that supports reconfiguration after construction. Implements ILogger directly,
/// eliminating the need for a separate decorator. Registered as SingleInstance because it is a
/// shared mutable primitive (same category as LoggingLevelSwitch).
/// </summary>
/// <remarks>
/// The CLI writes no log files. Until command setup runs, only errors reach the console, so a
/// failure before setup is still visible. Setup then either sends everything at the selected
/// level to the console (log mode) or discards it, because IAnsiConsole carries user output.
/// </remarks>
internal class ReloadableLogger(IEnvironment env, LoggingLevelSwitch levelSwitch) : ILogger
{
    private const string Template =
        "[{@l:u3}] {@m}{#if @x is not null}: {Inspect(@x).Message}{#end}\n";

    private volatile ILogger _inner = CreateConsoleLogger(
        env,
        new LoggingLevelSwitch(LogEventLevel.Error)
    );

    public bool IsEnabled(LogEventLevel level) => _inner.IsEnabled(level);

    public void Write(LogEvent logEvent)
    {
        _inner.Write(logEvent);
    }

    public void UseConsole()
    {
        _inner = CreateConsoleLogger(env, levelSwitch);
    }

    public void Silence()
    {
        _inner = Logger.None;
    }

    private static Logger CreateConsoleLogger(IEnvironment env, LoggingLevelSwitch levelSwitch)
    {
        var raw = !string.IsNullOrEmpty(env.GetEnvironmentVariable("NO_COLOR"));
        var template = new ExpressionTemplate(Template, theme: raw ? null : TemplateTheme.Code);

        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Console(template)
            .CreateLogger();
    }
}
