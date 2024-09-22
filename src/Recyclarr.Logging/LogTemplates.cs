namespace Recyclarr.Logging;

public static class LogTemplates
{
    public static string Base { get; } = GetBaseTemplateString();

    private static string GetBaseTemplateString()
    {
        var scope = LogProperty.Scope;

        return
            $"{{#if {scope} is not null}}{{{scope}}}: {{#end}}" +
            "{@m}" +
            "{#if SanitizedExceptionMessage is not null}: {SanitizedExceptionMessage}{#end}\n";
    }
}
