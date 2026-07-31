using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync;

internal sealed class SyncDiagnosticsLogger(ILogger log)
{
    public void Log(IReadOnlyCollection<DiagnosticEventResponse> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var instance = string.IsNullOrEmpty(diagnostic.Instance) ? null : diagnostic.Instance;
            var message = instance is null
                ? diagnostic.Message
                : $"[{instance}] {diagnostic.Message}";
            switch (diagnostic.Level)
            {
                case "Error":
                    log.Error("{Message}", message);
                    break;
                case "Deprecation":
                    log.Warning("[DEPRECATED] {Message}", message);
                    break;
                default:
                    log.Warning("{Message}", message);
                    break;
            }
        }
    }
}
