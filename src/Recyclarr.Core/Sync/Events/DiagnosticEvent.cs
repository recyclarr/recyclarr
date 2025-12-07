namespace Recyclarr.Sync.Events;

public record DiagnosticEvent(
    string? InstanceName,
    PipelineType? Pipeline,
    DiagnosticType Type,
    string Message
) : SyncEvent(InstanceName, Pipeline);
