using System.IO.Abstractions;
using Recyclarr.Common.Serilog;
using Recyclarr.TrashLib;
using Recyclarr.TrashLib.Startup;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Cli.Logging;

public class LoggerFactory
{
    private readonly IAppPaths _paths;
    private readonly LoggingLevelSwitch _levelSwitch;

    public LoggerFactory(IAppPaths paths, LoggingLevelSwitch levelSwitch)
    {
        _paths = paths;
        _levelSwitch = levelSwitch;
    }

    private static string GetBaseTemplateString()
    {
        var scope = LogProperty.Scope;

        return
            $"{{#if {scope} is not null}}{{{scope}}}: {{#end}}" +
            "{@m}";
    }

    private static ExpressionTemplate GetConsoleTemplate()
    {
        var template = "[{@l:u3}] " + GetBaseTemplateString() +
            "{#if ExceptionMessage is not null}: {ExceptionMessage}{#end}" +
            "\n";

        return new ExpressionTemplate(template, theme: TemplateTheme.Code);
    }

    private static ExpressionTemplate GetFileTemplate()
    {
        var template = "[{@t:HH:mm:ss} {@l:u3}] " + GetBaseTemplateString() + "\n{@x}";

        return new ExpressionTemplate(template);
    }

    public ILogger Create()
    {
        var logPath = _paths.LogDirectory.File($"trash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .Enrich.With<ExceptionMessageEnricher>()
            .WriteTo.Console(GetConsoleTemplate(), levelSwitch: _levelSwitch)
            .WriteTo.File(GetFileTemplate(), logPath.FullName)
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}
