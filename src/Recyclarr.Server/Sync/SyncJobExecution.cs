using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Quartz;
using Recyclarr.Config;
using Recyclarr.Server.Observability;
using Recyclarr.Server.Resources;
using Recyclarr.Settings.Models;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Quartz's payload for <see cref="SyncJobExecution"/>. Only the job id is queued; the job's
/// accepted request is read back from the job store when the job runs.
/// </summary>
internal sealed record SyncJobInput(Guid JobId);

/// <summary>
/// Runs one queued sync job when Quartz fires it. Quartz runs one job at a time, in creation order
/// (<see cref="SyncJobLauncher"/>). Each run holds a resource lease from start to finish, so guide data cannot change during the run.
/// </summary>
/// <remarks>
/// A run that exceeds <see cref="ServerSettings.SyncTimeout"/> is canceled and fails, which frees
/// the queue for the next job. Cancellation is cooperative: a sync stuck in code that ignores its
/// token keeps the queue blocked until it returns.
/// </remarks>
internal sealed class SyncJobExecution(
    ILogger log,
    ISyncJobStore store,
    ResourceCoordinator resources,
    SyncRunScopeFactory scopeFactory,
    SyncJobFinalizer finalizer,
    SyncJobTelemetry telemetry,
    IOptions<ServerSettings> settings,
    TimeProvider time
) : IJob<SyncJobInput>
{
    public ValueTask Execute(
        IJobExecutionContext context,
        SyncJobInput input,
        CancellationToken cancellationToken = default
    )
    {
        // A queued job must not inherit the async-local state (logging scope, activity) of the code
        // that started the worker; the job runs in a clean context instead.
        using (ExecutionContext.SuppressFlow())
        {
            return new ValueTask(
                Task.Run(() => RunJobAsync(input.JobId, cancellationToken), cancellationToken)
            );
        }
    }

    private async Task RunJobAsync(Guid jobId, CancellationToken shutdown)
    {
        // Jobs are queued only while pending, and only finished jobs are evicted from the store.
        var job =
            store.Get(new JobId { Value = jobId })
            ?? throw new InvalidOperationException($"Queued sync job {jobId} has no record");

        using var activity = SyncJobTelemetry.StartRun(job);
        var started = Stopwatch.GetTimestamp();

        await RunWithTimeLimitAsync(job, activity, shutdown);

        var finalStatus = store.Get(job.Id)?.Status ?? SyncJobStatus.Failed;
        if (finalStatus == SyncJobStatus.Failed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "sync job failed");
        }

        telemetry.RecordRun(job, finalStatus, Stopwatch.GetElapsedTime(started));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunWithTimeLimitAsync(SyncJob job, Activity? activity, CancellationToken ct)
    {
        var limit = TimeSpan.FromMinutes(settings.Value.SyncTimeout);
        using var timeout = new CancellationTokenSource(limit, time);
        using var run = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

        try
        {
            // Configuration files are fixed for the life of the process, so the snapshot holds the
            // instances the job was accepted with, resolved against current guide data.
            using var lease = await resources.AcquireReadAsync(run.Token);
            var configs = lease.Snapshot.Configuration.Select(
                job.Request.Service,
                job.Request.Instances
            );
            using var runScope = scopeFactory.Start<SyncJobRunner>(lease.Snapshot.Guide);
            await runScope.Entry.RunAsync(job.Id, configs, job.Request, run.Token);
        }
        catch (OperationCanceledException e) when (timeout.IsCancellationRequested)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(
                e,
                "Sync job {JobId} exceeded the {Minutes}-minute limit (server.sync_timeout) "
                    + "and was canceled ({Reference})",
                job.Id,
                limit.TotalMinutes,
                reference
            );
            activity?.SetStatus(ActivityStatusCode.Error, "timed out");
            finalizer.Fail(job.Id, new SyncFault(reference));
        }
        catch (OperationCanceledException e)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Information(e, "Sync job {JobId} was canceled ({Reference})", job.Id, reference);
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
            finalizer.Fail(job.Id, new SyncFault(reference));
        }
        catch (Exception e)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Unexpected sync job fault {Reference}", reference);
            activity?.SetStatus(ActivityStatusCode.Error, e.GetType().FullName);
            finalizer.Fail(job.Id, new SyncFault(reference));
        }
    }
}
