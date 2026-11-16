using Recyclarr.Server.Sync.Results;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

// Records a job's terminal state. Both operations are no-ops once the job is terminal (the store
// never mutates terminal jobs), so the first outcome recorded wins.
internal sealed class SyncJobFinalizer(ISyncJobStore store, TimeProvider time)
{
    public void Complete(JobId jobId, SyncRunResult result)
    {
        store.Update(
            jobId,
            job =>
            {
                var instances = result.Instances.Select(SyncJobResultsResponseMapper.MapInstance);
                job.Progress = job.Progress.Reconcile(instances).Stop();
                job.Status = result.Status.ToJobStatus();
                job.FaultReference = result.Fault?.Reference;
                job.FinishedAt = time.GetUtcNow();
            }
        );
    }

    // The run stopped on a fault: instances already finished keep their results. A fault before
    // the runner marked the job running (run scope setup) still counts as a started run.
    public void Fail(JobId jobId, SyncFault fault)
    {
        store.Update(
            jobId,
            job =>
            {
                if (job.Status != SyncJobStatus.Pending)
                {
                    return;
                }

                job.Status = SyncJobStatus.Running;
                job.StartedAt = time.GetUtcNow();
            }
        );

        store.Update(
            jobId,
            job =>
            {
                job.Progress = job.Progress.Stop();
                job.Status = job.Progress.HasCompletedWork
                    ? SyncJobStatus.Partial
                    : SyncJobStatus.Failed;
                job.FaultReference = fault.Reference;
                job.FinishedAt = time.GetUtcNow();
            }
        );
    }
}
