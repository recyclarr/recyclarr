using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config;
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
    SyncJobFinalizer finalizer
) : ITickerFunction<SyncJobTickerRequest>
{
    public async Task ExecuteAsync(
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
        await RunAsync(job, cancellationToken);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunAsync(SyncJob job, CancellationToken ct)
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
            finalizer.Fail(job.Id, new SyncFault(reference));
        }
        catch (Exception e)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Unexpected sync job fault {Reference}", reference);
            finalizer.Fail(job.Id, new SyncFault(reference));
        }
    }
}
