using Recyclarr.Logging;
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
internal class ReloadableLogger(IEnvironment env) : ILogger
{
    private volatile Logger _inner = BuildBootstrapLogger(env);

    public void Write(LogEvent logEvent)
    {
        _inner.Write(logEvent);
    }

    public void Reload(IEnumerable<ILogConfigurator> configurators)
    {
        var config = LogSetup.BaseConfiguration();

        foreach (var configurator in configurators)
        {
            configurator.Configure(config);
        }

        _inner = config.CreateLogger();
    }

    // Bootstrap logger: errors-only console output until fully configured
    private static Logger BuildBootstrapLogger(IEnvironment env)
    {
        var template = "[{@l:u3}] " + LogSetup.BaseTemplate;
        var raw = !string.IsNullOrEmpty(env.GetEnvironmentVariable("NO_COLOR"));
        var expressionTemplate = new ExpressionTemplate(
            template,
            theme: raw ? null : TemplateTheme.Code
        );

        return LogSetup
            .BaseConfiguration()
            .WriteTo.Console(expressionTemplate, restrictedToMinimumLevel: LogEventLevel.Error)
            .CreateLogger();
    }
}
