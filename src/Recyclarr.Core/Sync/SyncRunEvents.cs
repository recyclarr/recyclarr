using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public record PipelineEvent(
    string Instance,
    PipelineType Type,
    PipelineProgressStatus Status,
    int? Count
);

public record SyncDiagnosticEvent(string? Instance, SyncDiagnosticLevel Level, string Message);

public enum SyncDiagnosticLevel
{
    Error,
    Warning,
    Deprecation,
}
