namespace Recyclarr.Sync;

public interface ISyncRunResults
{
    SyncInstanceResult GetInstanceResult(string instanceName);
}
