using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Progress;

internal sealed class SyncJobProgress(JobId jobId, ISyncJobStore store) : IInstanceSyncProgress
{
    public void InstanceStarted(string instanceName)
    {
        store.Update(jobId, job => job.Progress = job.Progress.Start(instanceName));
    }

    public void InstanceCompleted(SyncInstanceResult result)
    {
        store.Update(jobId, job => job.Progress = job.Progress.Complete(result));
    }
}
