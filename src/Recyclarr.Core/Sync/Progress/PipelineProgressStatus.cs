namespace Recyclarr.Sync.Progress;

public enum PipelineProgressStatus
{
    Pending,
    Running,
    Succeeded,
    Partial,
    Failed,
    Skipped,
    Interrupted,
}
