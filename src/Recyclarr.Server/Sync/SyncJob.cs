using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

// Mutable job record updated in place by ISyncJobStore.Update while the sync runs in the
// background. Access is synchronized by the store, not by this type.
internal sealed class SyncJob(JobId id, ServerSyncSettings request, DateTimeOffset createdAt)
{
    public JobId Id { get; } = id;
    public ServerSyncSettings Request { get; } = request;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public ProgressSnapshot Progress { get; set; } = new([]);
    public IReadOnlyList<SyncDiagnosticEvent> Diagnostics { get; set; } = [];

    // Structured config-load diagnostics (unknown/invalid/duplicate/split instances, parse
    // failures, deprecations) captured when loading configs for this job. Null when config
    // loading produced no diagnostics. Distinct from Diagnostics above, which carries runtime
    // pipeline events emitted while the sync itself is executing.
    public ConfigLoadDiagnostics? ConfigDiagnostics { get; set; }

    public SyncRunResult? Result { get; set; }
}
