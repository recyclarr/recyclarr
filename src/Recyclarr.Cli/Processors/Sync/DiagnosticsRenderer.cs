using Recyclarr.Cli.Console.Widgets;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
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

            // A level this CLI doesn't know is still worth showing, so it falls back to a warning
            // rather than being dropped.
            var level = Enum.TryParse<SyncDiagnosticLevel>(d.Level, out var parsed)
                ? parsed
                : SyncDiagnosticLevel.Warning;

            switch (level)
            {
                case SyncDiagnosticLevel.Error:
                    panel.AddError(prefix, d.Message);
                    break;
                case SyncDiagnosticLevel.Deprecation:
                    panel.AddDeprecation(prefix, d.Message);
                    break;
                default:
                    panel.AddWarning(prefix, d.Message);
                    break;
            }
        }

        panel.Render(console);
    }
}
