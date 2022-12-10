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

    public LoggerFactory(IAppPaths paths)
    {
        _paths = paths;
    }

    private static string GetBaseTemplateString()
    {
        var scope = LogProperty.Scope;

        return
            $"{{#if {scope} is not null}}{{{scope}}}: {{#end}}" +
            "{@m}" +
            "{@x}" +
            "\n";
    }

    private static ExpressionTemplate GetConsoleTemplate()
    {
        var template = "[{@l:u3}] " + GetBaseTemplateString();
        return new ExpressionTemplate(template, theme: TemplateTheme.Code);
    }

    private static ExpressionTemplate GetFileTemplate()
    {
        var template = "[{@t:HH:mm:ss} {@l:u3}] " + GetBaseTemplateString();
        return new ExpressionTemplate(template);
    }

    public ILogger Create(LogEventLevel level)
    {
        var logPath = _paths.LogDirectory.File($"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.Console(GetConsoleTemplate(), level)
            .WriteTo.File(GetFileTemplate(), logPath.FullName)
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}
