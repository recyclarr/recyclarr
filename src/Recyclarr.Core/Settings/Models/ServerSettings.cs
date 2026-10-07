namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServerSettings
{
    public int Port { get; init; } = 7982;
    public string BindAddress { get; init; } = "localhost";
    public ScheduleSettings Schedule { get; init; } = new();
}

/// <summary>
/// The built-in sync schedule of a persistent server. The server parses the cron expression at
/// startup and evaluates it in the system time zone.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ScheduleSettings
{
    public bool Enabled { get; init; } = true;

    // Daily at midnight, matching the container's previous `@daily` cron job.
    public string Cron { get; init; } = "0 0 * * *";
}
