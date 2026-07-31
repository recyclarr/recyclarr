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

    /// <summary>
    /// Summarizes whether the pipeline completed all, some, or none of its intended work.
    /// </summary>
    public SyncResultStatus Status { get; }

    /// <summary>
    /// Identifies the direct dependency that prevented execution; present only when blocked.
    /// </summary>
    public PipelineType? BlockedBy { get; }

    internal abstract PipelineResult WithStatus(
        SyncResultStatus status,
        PipelineType? blockedBy = null
    );
}
