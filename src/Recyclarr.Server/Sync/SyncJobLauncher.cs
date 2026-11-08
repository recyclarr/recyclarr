using System.Diagnostics.CodeAnalysis;
using Quartz;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Queues a pending job for execution through Quartz (<see cref="SyncJobExecution"/>). Callers
/// follow the run through the job store.
/// </summary>
/// <remarks>
/// Quartz runs one job at a time and picks the earliest due trigger first, so each trigger is due
/// at its job's creation time: jobs run oldest first. Ceiling: two jobs created at the same instant
/// have no defined order.
/// </remarks>
internal sealed class SyncJobLauncher(
    ILogger log,
    ISyncJobStore store,
    IScheduler scheduler,
    SyncJobFinalizer finalizer
)
{
    // Not cancellable: a job left pending without a trigger would never run, and as an active job
    // it would cause every scheduled occurrence to be skipped until restart.
    [SuppressMessage(
        "Style",
        "REC001",
        Justification = "Explicit None marks this as uncancellable"
    )]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any scheduling failure must fail the job rather than leave it pending"
    )]
    public async Task LaunchAsync(JobId jobId)
    {
        try
        {
            var job =
                store.Get(jobId)
                ?? throw new InvalidOperationException($"Sync job {jobId} does not exist");

            await scheduler.ScheduleJob<SyncJobExecution, SyncJobInput>(
                new SyncJobInput(jobId.Value),
                job.CreatedAt,
                new OneOffJobOptions
                {
                    Name = jobId.Value.ToString(),
                    // Keeps the creation time as the due time of a job that waits behind another,
                    // so waiting jobs still run oldest first.
                    MisfireInstruction = SimpleTriggerMisfireInstruction.IgnoreMisfires,
                },
                CancellationToken.None
            );
        }
        catch (Exception e)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Sync job {JobId} could not be queued ({Reference})", jobId, reference);
            finalizer.Fail(jobId, new SyncFault(reference));
        }
    }
}
