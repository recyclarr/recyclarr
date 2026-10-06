using Microsoft.EntityFrameworkCore;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// Stored row for one sync job. Holds what the job API reports after a restart: the accepted
/// request, lifecycle timestamps, and per-instance progress with each finished instance's result.
/// </summary>
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
internal sealed class SyncJobRecord
{
    public Guid Id { get; set; }
    public SyncJobTrigger Trigger { get; set; }
    public SyncJobStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    // The cron occurrence a scheduled job was created for; null for manual jobs.
    public DateTimeOffset? ScheduledFor { get; set; }

    public SupportedServices? Service { get; set; }
    public IReadOnlyCollection<string> Instances { get; set; } = [];
    public bool Preview { get; set; }

    // For a skipped scheduled occurrence: the active job that caused the skip.
    public Guid? SkippedByJobId { get; set; }

    public string? FaultReference { get; set; }

    // The TickerQ time ticker that executes this job; null for jobs that never run (skipped).
    public Guid? TickerId { get; set; }

    public List<SyncJobInstanceRecord> Progress { get; set; } = [];
}

/// <summary>
/// Stored progress for one selected instance of a sync job, in selection order.
/// </summary>
[PrimaryKey(nameof(JobId), nameof(Ordinal))]
internal sealed class SyncJobInstanceRecord
{
    public Guid JobId { get; set; }

    // Inverse of SyncJobRecord.Progress; names the JobId foreign key by convention.
    public SyncJobRecord? Job { get; set; }

    public int Ordinal { get; set; }
    public required string Name { get; set; }
    public InstanceProgressStatus Status { get; set; }

    // The instance's result in its v1 API representation, serialized once the instance finishes.
    public string? ResultJson { get; set; }
}
