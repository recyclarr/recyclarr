namespace Recyclarr.Server.Sync;

// The fields a job listing reports; reading it does not load per-instance progress or results.
internal sealed record SyncJobSummary(
    JobId Id,
    SyncJobStatus Status,
    SyncJobTrigger Trigger,
    DateTimeOffset CreatedAt
);
