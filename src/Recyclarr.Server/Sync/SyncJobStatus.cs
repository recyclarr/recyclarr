using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Lifecycle of a sync job. Allowed transitions: <c>Pending</c> to <c>Running</c> or
/// <c>Interrupted</c>; <c>Running</c> to <c>Succeeded</c>, <c>Partial</c>, <c>Failed</c>, or
/// <c>Interrupted</c>. <c>Skipped</c> is created terminal. Terminal states never change.
/// </summary>
internal enum SyncJobStatus
{
    Pending,
    Running,
    Succeeded,
    Partial,
    Failed,

    // The server stopped before the job finished; finished instances keep their results.
    Interrupted,

    // A scheduled occurrence that overlapped an active job and never ran.
    Skipped,
}

internal static class SyncJobStatusExtensions
{
    extension(SyncJobStatus from)
    {
        public bool IsTerminal() => from is not (SyncJobStatus.Pending or SyncJobStatus.Running);

        // Staying in the same active status is allowed; it covers progress-only updates.
        public bool CanTransitionTo(SyncJobStatus to) =>
            (from, to) switch
            {
                (SyncJobStatus.Pending, SyncJobStatus.Pending or SyncJobStatus.Running) => true,
                (SyncJobStatus.Running, SyncJobStatus.Running) => true,
                (SyncJobStatus.Pending or SyncJobStatus.Running, SyncJobStatus.Interrupted) => true,
                (
                    SyncJobStatus.Running,
                    SyncJobStatus.Succeeded
                        or SyncJobStatus.Partial
                        or SyncJobStatus.Failed
                ) => true,
                _ => false,
            };
    }

    extension(SyncResultStatus status)
    {
        public SyncJobStatus ToJobStatus() =>
            status switch
            {
                SyncResultStatus.Succeeded => SyncJobStatus.Succeeded,
                SyncResultStatus.Partial => SyncJobStatus.Partial,
                _ => SyncJobStatus.Failed,
            };
    }
}
