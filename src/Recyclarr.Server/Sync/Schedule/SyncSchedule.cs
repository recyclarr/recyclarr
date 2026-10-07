using Cronos;
using Recyclarr.Server.Persistence;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Sync.Schedule;

/// <summary>
/// The effective built-in sync schedule. Both the scheduler's wait and the schedule endpoint's
/// next occurrence come from <see cref="NextOccurrence"/>, so they cannot disagree.
/// </summary>
/// <remarks>
/// Disabled when settings disable it or the server is ephemeral. The cron expression is parsed only
/// when enabled, so an invalid value fails startup only when it would be used. Occurrences are in
/// the system time zone (<c>TZ</c>, then <c>/etc/localtime</c> on Linux and macOS).
/// </remarks>
internal sealed class SyncSchedule
{
    private readonly CronExpression? _expression;

    private SyncSchedule(string cron, TimeZoneInfo timeZone, CronExpression? expression)
    {
        Cron = cron;
        TimeZone = timeZone;
        _expression = expression;
    }

    public bool Enabled => _expression is not null;
    public string Cron { get; }
    public TimeZoneInfo TimeZone { get; }

    public static SyncSchedule Create(ScheduleSettings settings, ServerMode mode, TimeProvider time)
    {
        if (!settings.Enabled || mode == ServerMode.Ephemeral)
        {
            return new SyncSchedule(settings.Cron, time.LocalTimeZone, null);
        }

        if (!CronExpression.TryParse(settings.Cron, out var expression))
        {
            throw new FatalException(
                $"`server.schedule.cron` is not a valid 5-field cron expression: {settings.Cron}"
            );
        }

        return new SyncSchedule(settings.Cron, time.LocalTimeZone, expression);
    }

    // Strictly after `now`; null when disabled or the expression has no later occurrence.
    public DateTimeOffset? NextOccurrence(DateTimeOffset now) =>
        _expression?.GetNextOccurrence(now, TimeZone);
}
