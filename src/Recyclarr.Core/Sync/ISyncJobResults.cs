namespace Recyclarr.Sync;

public interface ISyncJobResults
{
    SyncInstanceResult GetInstanceResult(JobId jobId, string instanceName);
}
