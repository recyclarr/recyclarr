namespace Recyclarr.Server.Sync.Schedule;

/// <summary>
/// Handles one schedule occurrence: queues a sync of every configured instance, or records the
/// occurrence as skipped when another job is pending or running. A skipped occurrence never runs.
/// </summary>
internal sealed class ScheduledSyncTrigger(
    ILogger log,
    ServerConfigurationStore configuration,
    ISyncJobStore store,
    SyncJobLauncher launcher
)
{
    public async Task TriggerAsync(DateTimeOffset occurrence)
    {
        var instances = configuration.Current.InstanceNames;
        if (instances.Count == 0)
        {
            log.Warning(
                "Scheduled sync at {Occurrence} not run: no instances are configured",
                occurrence
            );
            return;
        }

        var job = store.CreateScheduled(
            new ServerSyncSettings(Service: null, Instances: [], Preview: false),
            instances,
            occurrence
        );

        if (job.SkippedBy is { } activeJob)
        {
            log.Information(
                "Scheduled sync at {Occurrence} skipped: sync job {ActiveJobId} is still active",
                occurrence,
                activeJob
            );
            return;
        }

        log.Information("Scheduled sync at {Occurrence} queued as job {JobId}", occurrence, job.Id);
        await launcher.LaunchAsync(job.Id);
    }
}
