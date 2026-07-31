using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

internal enum SyncJobStatus
{
    Pending,
    Running,
    Succeeded,
    Partial,
    Failed,
}

internal static class SyncJobStatusExtensions
{
    public static bool IsTerminal(this SyncJobStatus status)
    {
        return status is SyncJobStatus.Succeeded or SyncJobStatus.Partial or SyncJobStatus.Failed;
    }

    public static SyncJobStatus ToJobStatus(this SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => SyncJobStatus.Succeeded,
            SyncResultStatus.Partial => SyncJobStatus.Partial,
            _ => SyncJobStatus.Failed,
        };
}
