using Recyclarr.Cli.Console.Widgets;
using Recyclarr.Client.V1;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

internal class DiagnosticsRenderer(IAnsiConsole console)
{
    public void Render(IReadOnlyCollection<DiagnosticEventResponse> diagnostics)
    {
        var panel = new DiagnosticPanel("Sync Diagnostics");

        foreach (var d in diagnostics)
        {
            var prefix = string.IsNullOrEmpty(d.Instance) ? null : d.Instance;

            switch (d.Level)
            {
                case "Error":
                    panel.AddError(prefix, d.Message);
                    break;
                case "Deprecation":
                    panel.AddDeprecation(prefix, d.Message);
                    break;
                // A level this CLI doesn't know is still worth showing.
                default:
                    panel.AddWarning(prefix, d.Message);
                    break;
            }
        }

        panel.Render(console);
    }
}
