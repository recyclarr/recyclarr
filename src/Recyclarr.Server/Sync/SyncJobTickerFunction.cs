using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config;
using Recyclarr.Server.Telemetry;
using Recyclarr.Sync.Results;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Recyclarr.Server.Sync;

/// <summary>
/// TickerQ's payload for <see cref="SyncJobTickerFunction"/>. Only the job id is queued; the job's
/// accepted request is read back from the job store when the ticker runs.
/// </summary>
internal sealed record SyncJobTickerRequest(Guid JobId);

/// <summary>
/// Runs one queued sync job when TickerQ executes its ticker. Jobs run one at a time in creation
/// order (<see cref="SyncExecutionGate"/>). A job that is already terminal is not run: a ticker
/// left over from a previous process finds its job interrupted by startup recovery.
/// </summary>
internal sealed class SyncJobTickerFunction(
    ILogger log,
    ISyncJobStore store,
    ServerConfigurationStore configuration,
    SyncExecutionGate gate,
    SyncRunScopeFactory scopeFactory,
    SyncJobFinalizer finalizer,
    SyncJobTelemetry telemetry
) : ITickerFunction<SyncJobTickerRequest>
{
    public Task ExecuteAsync(
        TickerFunctionContext<SyncJobTickerRequest> context,
        CancellationToken cancellationToken
    )
    {
        // TickerQ workers inherit the execution context of whatever created them, often an HTTP
        // request. Without isolation, that request's async-local state (logging scope, activity)
        // would tag every log line and span of the job. The job runs in a clean context instead.
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(() => RunJobAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task RunJobAsync(
        TickerFunctionContext<SyncJobTickerRequest> context,
        CancellationToken cancellationToken
    )
    {
        var job = store.Get(new JobId { Value = context.Request.JobId });
        if (job is null || job.Status.IsTerminal())
        {
            log.Debug(
                "Sync job {JobId} is gone or finished; not running it",
                context.Request.JobId
            );
            return;
        }

        // Canceled while waiting (shutdown): the job stays pending, and startup recovery marks it
        // interrupted on the next start.
        using var admission = await gate.EnterAsync(job, cancellationToken);
        using var activity = SyncJobTelemetry.StartRun(job);
        var started = Stopwatch.GetTimestamp();

        await RunAsync(job, activity, cancellationToken);

        var finalStatus = store.Get(job.Id)?.Status ?? SyncJobStatus.Failed;
        if (finalStatus == SyncJobStatus.Failed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "sync job failed");
        }

        telemetry.RecordRun(job, finalStatus, Stopwatch.GetElapsedTime(started));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunAsync(SyncJob job, Activity? activity, CancellationToken ct)
    {
        try
        {
            // Configuration is fixed for the life of the process, so this selects what the job was
            // accepted with.
            var configs = configuration.Current.Select(job.Request.Service, job.Request.Instances);
            using var runScope = scopeFactory.Start<SyncJobRunner>();
            await runScope.Entry.RunAsync(job.Id, configs, job.Request, ct);
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
