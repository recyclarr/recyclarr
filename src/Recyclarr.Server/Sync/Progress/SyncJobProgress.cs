using Recyclarr.Server.Sync.Results;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Progress;

// Run-scoped: one instance per run, so Completed holds only this run's results.
internal sealed class SyncJobProgress(JobId jobId, ISyncJobStore store) : IInstanceSyncProgress
{
    private readonly List<SyncInstanceResult> _completed = [];

    // Domain results of instances finished so far, for reporting a run that stops on a fault.
    public IReadOnlyList<SyncInstanceResult> Completed => _completed;

    public void InstanceStarted(string instanceName)
    {
        store.Update(jobId, job => job.Progress = job.Progress.Start(instanceName));
    }

    public void InstanceCompleted(SyncInstanceResult result)
    {
        _completed.Add(result);
        var response = SyncJobResultsResponseMapper.MapInstance(result);
        store.Update(jobId, job => job.Progress = job.Progress.Complete(response));
    }
}
