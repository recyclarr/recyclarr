using Serilog.Events;

namespace Recyclarr.Logging;

public static class LogSetup
{
    public static string BaseTemplate { get; } = GetBaseTemplateString();

    public static string FileTemplate { get; } =
        "[{@t:HH:mm:ss} {@l:u3}] "
        + BaseTemplate
        + "{#if @x is not null}{Inspect(@x).StackTrace}\n{#end}";

    public static LoggerConfiguration BaseConfiguration()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext()
            .Enrich.With<ExceptionSanitizingEnricher>();
    }

    private static string GetBaseTemplateString()
    {
        var scope = LogProperty.Scope;

        return $"{{#if {scope} is not null}}{{{scope}}}: {{#end}}"
            + "{@m}"
            + "{#if SanitizedExceptionMessage is not null}: {SanitizedExceptionMessage}{#end}\n";
    }
}
