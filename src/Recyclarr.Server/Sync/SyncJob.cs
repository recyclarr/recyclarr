using Recyclarr.Server.Sync.Progress;

namespace Recyclarr.Server.Sync;

/// <summary>
/// One sync job as read from <see cref="ISyncJobStore"/>. Every read returns a new instance;
/// mutable members exist only for the mutation callback of <see cref="ISyncJobStore.Update"/>,
/// which the store persists afterwards.
/// </summary>
internal sealed class SyncJob
{
    public required JobId Id { get; init; }
    public required SyncJobTrigger Trigger { get; init; }
    public required ServerSyncSettings Request { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // The schedule occurrence a scheduled job was created for; null for manual jobs.
    public DateTimeOffset? ScheduledFor { get; init; }

    // For a skipped scheduled occurrence: the active job that caused the skip.
    public JobId? SkippedBy { get; init; }

    public required SyncJobStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public required ProgressSnapshot Progress { get; set; }

    // Correlates a run-level fault with its log entry.
    public string? FaultReference { get; set; }
}
