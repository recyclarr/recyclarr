namespace Recyclarr.Sync.Progress;

public readonly record struct PipelineSnapshot(PipelineProgressStatus Status, int? Count);
