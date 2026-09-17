using Recyclarr.Sync.Results;

namespace Recyclarr.Sync;

/// <summary>
/// Receives ordered lifecycle changes for service instances in one sync run.
/// </summary>
/// <remarks>
/// Completion is reported only after instance cleanup has produced its final result. Implementations
/// must not let reporting failures alter execution or terminal results.
/// </remarks>
public interface IInstanceSyncProgress
{
    void InstanceStarted(string instanceName);
    void InstanceCompleted(SyncInstanceResult result);
}
