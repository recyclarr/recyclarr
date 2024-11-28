using Serilog.Events;

namespace Recyclarr.Logging;

public static class LogSetup
{
    public static string BaseTemplate { get; } = GetBaseTemplateString();

    public static LoggerConfiguration BaseConfiguration()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .Enrich.With<FlurlExceptionSanitizingEnricher>();
    }

    private static string GetBaseTemplateString()
    {
        var scope = LogProperty.Scope;

        return $"{{#if {scope} is not null}}{{{scope}}}: {{#end}}"
            + "{@m}"
            + "{#if SanitizedExceptionMessage is not null}: {SanitizedExceptionMessage}{#end}\n";
    }
}
