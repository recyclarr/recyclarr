using System.IO.Abstractions;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Recyclarr.Cli.Logging;

public class LoggerFactory(IAppPaths paths, LoggingLevelSwitch levelSwitch)
{
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
            "{#if SanitizedExceptionMessage is not null}: {SanitizedExceptionMessage}{#end}\n";

        return new ExpressionTemplate(template, theme: TemplateTheme.Code);
    }

    private static ExpressionTemplate GetFileTemplate()
    {
        var template = "[{@t:HH:mm:ss} {@l:u3}] " + GetBaseTemplateString() +
            "{#if SanitizedExceptionFull is not null}: {SanitizedExceptionFull}{#end}\n";

        return new ExpressionTemplate(template);
    }

    public ILogger Create()
    {
        var logFilePrefix = $"recyclarr_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        var logDir = paths.LogDirectory;

        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .Enrich.With<FlurlExceptionSanitizingEnricher>()
            .WriteTo.Console(GetConsoleTemplate(), levelSwitch: levelSwitch)
            .WriteTo.Logger(c => c
                .MinimumLevel.Debug()
                .WriteTo.File(GetFileTemplate(), LogFilePath("debug")))
            .WriteTo.Logger(c => c
                .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                .WriteTo.File(GetFileTemplate(), LogFilePath("verbose")))
            .CreateLogger();

        string LogFilePath(string type)
        {
            return logDir.File($"{logFilePrefix}.{type}.log").FullName;
        }
    }
}
