using Recyclarr.Cli.Console.Widgets;
using Recyclarr.Sync;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

internal class DiagnosticsRenderer : IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly List<SyncDiagnosticEvent> _diagnostics = [];
    private readonly IDisposable _subscription;

    public DiagnosticsRenderer(IAnsiConsole console, ISyncRunScope run)
    {
        _console = console;
        _subscription = run.Diagnostics.Subscribe(_diagnostics.Add);
    }

    public void Report()
    {
        var panel = new DiagnosticPanel("Sync Diagnostics");

        foreach (var d in _diagnostics)
        {
            var prefix = string.IsNullOrEmpty(d.Instance) ? null : d.Instance;

            switch (d.Level)
            {
                case SyncDiagnosticLevel.Error:
                    panel.AddError(prefix, d.Message);
                    break;
                case SyncDiagnosticLevel.Warning:
                    panel.AddWarning(prefix, d.Message);
                    break;
                case SyncDiagnosticLevel.Deprecation:
                    panel.AddDeprecation(prefix, d.Message);
                    break;
            }
        }

        panel.Render(_console);
    }

    public void Dispose() => _subscription.Dispose();
}
