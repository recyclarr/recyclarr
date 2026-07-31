using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Sync;

namespace Recyclarr.Server.Sync;

/// <summary>
/// Creates a sync job and starts the run behind it. Callers get a job record back immediately and
/// follow the rest through the store; the run's lifetime scope and background task are this class's
/// business alone.
/// </summary>
internal sealed class SyncJobLauncher(ISyncJobStore store, SyncRunScopeFactory scopeFactory)
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
        catch (Exception e)
        {
            store.Update(
                jobId,
                j =>
                {
                    j.Status = SyncJobStatus.Failed;
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
