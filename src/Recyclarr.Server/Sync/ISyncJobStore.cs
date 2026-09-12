namespace Recyclarr.Server.Sync;

internal interface ISyncJobStore
{
    SyncJob Create(ServerSyncSettings request);

    // Reads return a stable job envelope. Immutable progress and result snapshots are shared.
    SyncJob? Get(JobId id);

    IReadOnlyList<SyncJob> GetAll(SyncJobStatus? statusFilter);

    void Update(JobId id, Action<SyncJob> mutate);
}
