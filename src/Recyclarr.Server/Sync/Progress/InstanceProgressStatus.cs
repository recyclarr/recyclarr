namespace Recyclarr.Server.Sync.Progress;

internal enum InstanceProgressStatus
{
    Pending,
    Running,
    Succeeded,
    Partial,
    Failed,
    Interrupted,
    NotRun,
}
