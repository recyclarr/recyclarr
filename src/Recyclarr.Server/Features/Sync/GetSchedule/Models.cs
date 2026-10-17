namespace Recyclarr.Server.Features.Sync.GetSchedule;

/// <summary>The built-in sync schedule.</summary>
internal sealed record SyncScheduleResponse
{
    /// <summary>True when the schedule creates sync jobs.</summary>
    public required bool Enabled { get; init; }

    /// <summary>The cron expression that sets when scheduled jobs are created.</summary>
    public required string Cron { get; init; }

    /// <summary>The time zone the cron expression uses (for example `America/Chicago`).</summary>
    public required string TimeZone { get; init; }

    /// <summary>
    /// The scheduled time of the most recent scheduled job the server still keeps, whether it ran
    /// or was skipped; absent when there is none.
    /// </summary>
    public DateTimeOffset? PreviousOccurrence { get; init; }

    /// <summary>The next scheduled time; absent while the schedule is disabled.</summary>
    public DateTimeOffset? NextOccurrence { get; init; }
}
