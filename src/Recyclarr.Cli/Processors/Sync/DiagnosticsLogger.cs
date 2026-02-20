using Recyclarr.Sync;

namespace Recyclarr.Cli.Processors.Sync;

internal class DiagnosticsLogger(ILogger log, ISyncRunScope run) : IDisposable
{
    private readonly IDisposable _subscription = run.Diagnostics.Subscribe(e =>
    {
        switch (e.Level)
        {
            case SyncDiagnosticLevel.Error:
                log.Error("{Message}", e.Message);
                break;
            case SyncDiagnosticLevel.Warning:
                log.Warning("{Message}", e.Message);
                break;
            case SyncDiagnosticLevel.Deprecation:
                log.Warning("[DEPRECATED] {Message}", e.Message);
                break;
        }
    });

    public void Dispose() => _subscription.Dispose();
}
