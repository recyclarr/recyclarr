namespace Recyclarr.Sync.Events;

public record CompletionEvent(string? InstanceName, PipelineType? Pipeline, int Count)
    : SyncEvent(InstanceName, Pipeline);
