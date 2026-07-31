namespace Recyclarr.Sync.Results;

/// <summary>
/// Common terminal state for a pipeline-owned result.
/// </summary>
public abstract record PipelineResult
{
    protected PipelineResult(SyncResultStatus status, PipelineType? blockedBy = null)
    {
        if ((status is SyncResultStatus.Blocked) != blockedBy.HasValue)
        {
            throw new ArgumentException(
                "Only blocked pipelines must identify a blocking dependency.",
                nameof(blockedBy)
            );
        }

        Status = status;
        BlockedBy = blockedBy;
    }

    public SyncResultStatus Status { get; }
    public PipelineType? BlockedBy { get; }
}
