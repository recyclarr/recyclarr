using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Models;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

// Entry point resolved inside a run's lifetime scope (see SyncRunScopeFactory). Reduces instance
// lifecycle callbacks into the job store and records the terminal result.
internal sealed class SyncJobRunner(
    ILogger log,
    ISyncOrchestrator orchestrator,
    ISyncJobStore store,
    INotificationService notify,
    SyncResultLogger resultLogger,
    Func<JobId, SyncJobProgress> progressFactory,
    SyncJobFinalizer finalizer
)
{
    public async Task RunAsync(
        JobId jobId,
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        store.Update(jobId, job => job.Status = SyncJobStatus.Running);

        SyncRunResult result;

        try
        {
            result = await orchestrator.RunAsync(configs, settings, progressFactory(jobId), ct);
            finalizer.Complete(jobId, result);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Unexpected sync runner fault {Reference}", reference);
            result = finalizer.Fail(jobId, new SyncFault(reference));
        }

        resultLogger.Log(jobId, result);

        await SendNotificationAsync(result);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task SendNotificationAsync(SyncRunResult result)
    {
        try
        {
            await notify.SendNotification(result);
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to send notification");
        }
    }
}
