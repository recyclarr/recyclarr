using Recyclarr.Sync.Results;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Queues a pending job for execution through TickerQ (<see cref="SyncJobTickerFunction"/>).
/// Callers follow the run through the job store.
/// </summary>
internal sealed class SyncJobLauncher(
    ILogger log,
    ISyncJobStore store,
    ITimeTickerManager<TimeTickerEntity> tickers,
    SyncJobFinalizer finalizer,
    TimeProvider time
)
{
    // Not cancellable: a job left pending without a ticker would never run, and as an active job
    // it would cause every scheduled occurrence to be skipped until restart.
    public async Task LaunchAsync(JobId jobId)
    {
        var result = await tickers.AddAsync<SyncJobTickerFunction, SyncJobTickerRequest>(
            time.GetUtcNow().UtcDateTime,
            new SyncJobTickerRequest(jobId.Value),
            CancellationToken.None
        );

        if (result.IsSucceeded)
        {
            store.AssignTicker(jobId, result.Result.Id);
            return;
        }

        var reference = Guid.NewGuid().ToString("N");
        log.Error(
            result.Exception,
            "Sync job {JobId} could not be queued ({Reference})",
            jobId,
            reference
        );
        finalizer.Fail(jobId, new SyncFault(reference));
    }
}
