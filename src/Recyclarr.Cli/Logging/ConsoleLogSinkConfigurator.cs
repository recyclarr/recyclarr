using Recyclarr.Logging;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Cli.Logging;

internal class ConsoleLogSinkConfigurator(IEnvironment env, LoggingLevelSwitch levelSwitch)
    : ILogConfigurator
{
    public void Configure(LoggerConfiguration config)
    {
        config.WriteTo.Console(BuildExpressionTemplate(), levelSwitch: levelSwitch);
    }

    private ExpressionTemplate BuildExpressionTemplate()
    {
        var template = "[{@l:u3}] " + LogSetup.BaseTemplate;
        var raw = !string.IsNullOrEmpty(env.GetEnvironmentVariable("NO_COLOR"));
        return new ExpressionTemplate(template, theme: raw ? null : TemplateTheme.Code);
    }
}
