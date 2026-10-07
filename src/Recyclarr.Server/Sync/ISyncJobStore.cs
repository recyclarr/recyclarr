namespace Recyclarr.Server.Sync;

/// <summary>
/// Durable sync job storage. Jobs survive server restarts; terminal jobs beyond the retention
/// limit are evicted oldest first, while active jobs are never evicted.
/// </summary>
internal interface ISyncJobStore
{
    SyncJob Create(ServerSyncSettings request, IReadOnlyList<string> instanceNames);

    /// <summary>
    /// Creates the job for one schedule occurrence. When another job is pending or running, the
    /// job is created <see cref="SyncJobStatus.Skipped"/> and names that job; the check and the
    /// insert are atomic.
    /// </summary>
    SyncJob CreateScheduled(
        ServerSyncSettings request,
        IReadOnlyList<string> instanceNames,
        DateTimeOffset scheduledFor
    );

    SyncJob? Get(JobId id);

    // Oldest first.
    IReadOnlyList<SyncJobSummary> GetAll(SyncJobStatus? status, SyncJobTrigger? trigger);

    // The occurrence of the most recent scheduled job still retained, whether it ran or was skipped.
    DateTimeOffset? LatestScheduledOccurrence();

    /// <summary>
    /// Applies <paramref name="mutate"/> to the job and persists the result. Read, mutation, and
    /// write are atomic. Terminal jobs are never mutated; the call is then a no-op. A status change
    /// that <see cref="SyncJobStatusExtensions.CanTransitionTo"/> rejects throws
    /// <see cref="InvalidOperationException"/> and persists nothing.
    /// </summary>
    void Update(JobId id, Action<SyncJob> mutate);

    /// <summary>
    /// Records the TickerQ ticker that executes the job, so evicting the job also removes the
    /// ticker. Applies whatever the job's status.
    /// </summary>
    void AssignTicker(JobId id, Guid tickerId);

    /// <summary>
    /// Marks every pending or running job <see cref="SyncJobStatus.Interrupted"/>. Only valid at
    /// startup, before any job can run.
    /// </summary>
    void InterruptActive();
}
