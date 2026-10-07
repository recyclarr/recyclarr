using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Server.Sync.Schedule;

/// <summary>
/// Creates sync jobs on the built-in schedule (<see cref="SyncSchedule"/>). Each next occurrence is
/// computed from the current time, so occurrences missed while the server was down (or the clock
/// jumped) are never replayed.
/// </summary>
/// <remarks>
/// The schedule is resolved in <see cref="StartAsync"/>, after server bootstrap has migrated
/// settings, so an invalid schedule fails startup. The first occurrence is fixed there too, so it
/// does not depend on when the background loop gets to run.
/// </remarks>
internal sealed class SyncScheduler(
    ILogger log,
    Lazy<SyncSchedule> schedule,
    ScheduledSyncTrigger trigger,
    TimeProvider time
) : BackgroundService
{
    // Task.Delay rejects waits longer than about 49 days; longer waits are taken in steps.
    private static readonly TimeSpan MaxWait = TimeSpan.FromDays(1);

    private DateTimeOffset? _firstOccurrence;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var current = schedule.Value;
        if (!current.Enabled)
        {
            log.Information("Built-in sync schedule is disabled");
            return Task.CompletedTask;
        }

        _firstOccurrence = current.NextOccurrence(time.GetUtcNow());
        log.Information(
            "Sync schedule {Cron} ({TimeZone}); next occurrence {Next}",
            current.Cron,
            current.TimeZone.Id,
            _firstOccurrence
        );
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var next = _firstOccurrence;
        while (next is { } occurrence)
        {
            await WaitUntilAsync(occurrence, stoppingToken);
            await TriggerAsync(occurrence);
            next = schedule.Value.NextOccurrence(time.GetUtcNow());
        }

        log.Warning("Sync schedule has no further occurrences");
    }

    private async Task WaitUntilAsync(DateTimeOffset due, CancellationToken ct)
    {
        for (var now = time.GetUtcNow(); now < due; now = time.GetUtcNow())
        {
            var remaining = due - now;
            await Task.Delay(remaining < MaxWait ? remaining : MaxWait, time, ct);
        }
    }

    // A failed occurrence must not end the schedule.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task TriggerAsync(DateTimeOffset occurrence)
    {
        try
        {
            await trigger.TriggerAsync(occurrence);
        }
        catch (Exception e)
        {
            log.Error(e, "Scheduled sync at {Occurrence} failed to start", occurrence);
        }
    }
}
