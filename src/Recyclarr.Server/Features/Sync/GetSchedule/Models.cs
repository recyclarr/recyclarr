namespace Recyclarr.Server.Features.Sync.GetSchedule;

/// <summary>
/// The built-in sync schedule. <c>PreviousOccurrence</c> is the most recent retained scheduled job
/// (run or skipped); <c>NextOccurrence</c> is absent while the schedule is disabled.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncScheduleResponse(
    bool Enabled,
    string Cron,
    string TimeZone,
    DateTimeOffset? PreviousOccurrence,
    DateTimeOffset? NextOccurrence
);
