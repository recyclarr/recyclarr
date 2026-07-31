using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public record PipelineEvent(
    string Instance,
    PipelineType Type,
    PipelineProgressStatus Status,
    int? Count,
    PipelineItemChanges? Changes = null
);

public record PipelineItemChanges(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Deleted
);

public record SyncDiagnosticEvent(
    string? Instance,
    SyncDiagnosticLevel Level,
    string Message,
    SyncOutcome? Outcome = null
);

public enum SyncDiagnosticLevel
{
    Error,
    Warning,
    Deprecation,
}
