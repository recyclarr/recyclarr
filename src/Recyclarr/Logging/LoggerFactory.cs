using System.IO.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using TrashLib.Startup;

namespace Recyclarr.Logging;

public class LoggerFactory
{
    private readonly IAppPaths _paths;

    private const string BaseTemplate =
        "{@m}" +
        "{@x}" +
        "\n";

    public LoggerFactory(IAppPaths paths)
    {
        _paths = paths;
    }

    private static ExpressionTemplate GetConsoleTemplate()
    {
        const string template =
            "[{@l:u3}] " +
            BaseTemplate;

        return new ExpressionTemplate(template, theme: TemplateTheme.Code);
    }

    private static ExpressionTemplate GetFileTemplate()
    {
        const string template =
            "[{@t:HH:mm:ss} {@l:u3}] " +
            BaseTemplate;

        return new ExpressionTemplate(template);
    }

    public ILogger Create(LogEventLevel level)
    {
        var logPath = _paths.LogDirectory.File($"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.Console(GetConsoleTemplate(), level)
            .WriteTo.File(GetFileTemplate(), logPath.FullName)
            .CreateLogger();
    }
}
