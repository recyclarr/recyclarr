using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

// Base type exists solely for Observable.Merge in progress renderer.
// Not used for polymorphic dispatch; consumers subscribe to typed observables.
public abstract record SyncRunEvent;

public record InstanceEvent(string Name, InstanceProgressStatus Status) : SyncRunEvent;

public record PipelineEvent(
    string Instance,
    PipelineType Type,
    PipelineProgressStatus Status,
    int? Count
) : SyncRunEvent;

public record SyncDiagnosticEvent(string? Instance, SyncDiagnosticLevel Level, string Message)
    : SyncRunEvent;

public enum SyncDiagnosticLevel
{
    Error,
    Warning,
    Deprecation,
}
