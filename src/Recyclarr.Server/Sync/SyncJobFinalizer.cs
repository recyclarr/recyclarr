using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

internal sealed class SyncJobFinalizer(ISyncJobStore store)
{
    public void Complete(JobId jobId, SyncRunResult result)
    {
        store.Update(
            jobId,
            job =>
            {
                if (job.Result is not null)
                {
                    return;
                }

                job.Progress = job.Progress.Reconcile(result).Stop();
                job.Result = result;
                job.Status = result.Status.ToJobStatus();
            }
        );
    }

    public SyncRunResult Fail(JobId jobId, SyncFault fault)
    {
        SyncRunResult? finalResult = null;
        store.Update(
            jobId,
            job =>
            {
                if (job.Result is not null)
                {
                    finalResult = job.Result;
                    return;
                }

                var completed = job
                    .Progress.Instances.Select(instance => instance.Result)
                    .OfType<SyncInstanceResult>()
                    .ToList();
                finalResult = new SyncRunResult(completed, fault);
                job.Progress = job.Progress.Stop();
                job.Result = finalResult;
                job.Status = finalResult.Status.ToJobStatus();
            }
        );

        return finalResult ?? new SyncRunResult([], fault);
    }
}
