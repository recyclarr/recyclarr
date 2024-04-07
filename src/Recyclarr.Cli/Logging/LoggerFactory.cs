using Recyclarr.Logging;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Cli.Logging;

public class LoggerFactory(IEnvironment env, LoggingLevelSwitch levelSwitch)
{
    public ILogger Logger { get; private set; } = LogSetup.BaseConfiguration()
        .WriteTo.Console(BuildExpressionTemplate(env), levelSwitch: levelSwitch)
        .CreateLogger();

    private static ExpressionTemplate BuildExpressionTemplate(IEnvironment env)
    {
        var template = "[{@l:u3}] " + LogSetup.BaseTemplate;

        var raw = !string.IsNullOrEmpty(env.GetEnvironmentVariable("NO_COLOR"));
        return new ExpressionTemplate(template, theme: raw ? null : TemplateTheme.Code);
    }

    public void AddLogConfiguration(IEnumerable<ILogConfigurator> configurators)
    {
        var config = LogSetup.BaseConfiguration()
            .WriteTo.Logger(Logger);

        foreach (var configurator in configurators)
        {
            configurator.Configure(config);
        }

        Logger = config.CreateLogger();
    }
}
