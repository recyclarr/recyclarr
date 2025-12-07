namespace Recyclarr.Sync.Events;

public abstract record SyncEvent(string? InstanceName, PipelineType? Pipeline);
