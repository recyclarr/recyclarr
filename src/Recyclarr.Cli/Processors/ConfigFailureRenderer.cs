using Recyclarr.Cli.Console.Widgets;
using Recyclarr.Config.Parsing.ErrorHandling;
using Spectre.Console;

namespace Recyclarr.Cli.Processors;

internal static class ConfigFailureRenderer
{
    public static void Render(IAnsiConsole console, ILogger log, ConfigRegistryResult result)
    {
        var panel = new DiagnosticPanel("Config Diagnostics");

        foreach (var failure in result.Failures)
        {
            var file = failure.FilePath?.Name ?? "unknown";
            var message = failure.Message;

            panel.AddError(file, message);
            log.Error(failure, "Config parsing failed in {File}: {Message}", file, message);
        }

        foreach (var message in result.DeprecationWarnings)
        {
            panel.AddDeprecation(null, message);
            log.Warning("[DEPRECATED] {Message}", message);
        }

        panel.Render(console);
    }
}
