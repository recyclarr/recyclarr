using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using InstanceProgressSnapshot = Recyclarr.Server.Sync.Progress.ProgressSnapshot;

namespace Recyclarr.Server.Sync;

// Mutable job record updated in place by ISyncJobStore.Update while the sync runs in the
// background. Access is synchronized by the store, not by this type.
internal sealed class SyncJob(
    JobId id,
    ServerSyncSettings request,
    DateTimeOffset createdAt,
    IReadOnlyList<string> instanceNames
)
{
    public JobId Id { get; } = id;
    public ServerSyncSettings Request { get; } = request;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public InstanceProgressSnapshot Progress { get; set; } = new(instanceNames);
    public IReadOnlyList<SyncDiagnosticEvent> Diagnostics { get; set; } = [];

    public SyncRunResult? Result { get; set; }

    internal SyncJob Snapshot() =>
        new(Id, Request, CreatedAt, [])
        {
            Status = Status,
            Progress = Progress,
            Diagnostics = Diagnostics.ToList(),
            Result = Result,
        };
}
