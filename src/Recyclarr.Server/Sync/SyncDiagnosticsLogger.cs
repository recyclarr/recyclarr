using Recyclarr.Sync;

namespace Recyclarr.Server.Sync;

internal sealed class SyncDiagnosticsLogger(ILogger log, ISyncRunScope run) : IDisposable
{
    private readonly IDisposable _subscription = run.Diagnostics.Subscribe(diagnostic =>
    {
        switch (diagnostic.Level)
        {
            case SyncDiagnosticLevel.Error:
                log.Error("{Message}", diagnostic.Message);
                break;
            case SyncDiagnosticLevel.Warning:
                log.Warning("{Message}", diagnostic.Message);
                break;
            case SyncDiagnosticLevel.Deprecation:
                log.Warning("[DEPRECATED] {Message}", diagnostic.Message);
                break;
        }
    });

    public void Dispose() => _subscription.Dispose();
}
