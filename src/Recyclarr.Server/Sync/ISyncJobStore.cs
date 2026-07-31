namespace Recyclarr.Server.Sync;

internal interface ISyncJobStore
{
    SyncJob Create(ServerSyncSettings request);

    SyncJob? Get(JobId id);

    IReadOnlyList<SyncJob> GetAll(SyncJobStatus? statusFilter);

    void Update(JobId id, Action<SyncJob> mutate);
}
