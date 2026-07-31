using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Creates a sync job and starts the run behind it. Callers get a job record back immediately and
/// follow the rest through the store; the run's lifetime scope and background task are this class's
/// business alone.
/// </summary>
internal sealed class SyncJobLauncher(
    ILogger log,
    ISyncJobStore store,
    SyncRunScopeFactory scopeFactory
)
{
    public SyncJob Launch(
        ServerSyncSettings settings,
        ConfigLoadDiagnostics configDiagnostics,
        IReadOnlyList<IServiceConfiguration> configs
    )
    {
        var job = store.Create(settings);

        if (!configDiagnostics.IsEmpty)
        {
            store.Update(job.Id, j => j.ConfigDiagnostics = configDiagnostics);
        }

        var runScope = scopeFactory.Start<SyncJobRunner>();

        // Runs independently of whatever asked for the job; progress and the terminal result are
        // recorded in the store and observed through the job resource.
        _ = Task.Run(() => RunAsync(runScope, job.Id, configs, settings), CancellationToken.None);

        return job;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunAsync(
        LifetimeScopeWrapper<SyncJobRunner> runScope,
        JobId jobId,
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings
    )
    {
        try
        {
            using (runScope)
            {
                await runScope.Entry.RunAsync(jobId, configs, settings, CancellationToken.None);
            }
        }
        catch (OperationCanceledException e)
        {
            log.Information(e, "Sync job {JobId} was canceled", jobId);
            store.Update(
                jobId,
                j =>
                {
                    if (j.Result is null)
                    {
                        j.Status = SyncJobStatus.Failed;
                    }
                }
            );
        }
        catch (Exception e)
        {
            var reference = Guid.NewGuid().ToString("N");
            log.Error(e, "Unexpected sync launcher fault {Reference}", reference);
            var result = new SyncRunResult([], new SyncFault(reference));
            store.Update(
                jobId,
                j =>
                {
                    if (j.Result is null)
                    {
                        j.Result = result;
                        j.Status = result.Status.ToJobStatus();
                    }

                    j.Diagnostics =
                    [
                        .. j.Diagnostics,
                        new SyncDiagnosticEvent(null, SyncDiagnosticLevel.Error, e.Message),
                    ];
                }
            );
        }
    }
}
