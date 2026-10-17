namespace Recyclarr.Settings.Models;

public record ServerSettings
{
    public int Port { get; init; } = 7982;
    public string BindAddress { get; init; } = "localhost";
    public ScheduleSettings Schedule { get; init; } = new();
    public ManagementSettings Management { get; init; } = new();
}

/// <summary>
/// The persistent server's management port, which alone serves health probes and Prometheus
/// metrics (ADR-029). Its default is fixed rather than derived from the API port.
/// </summary>
public record ManagementSettings
{
    public int Port { get; init; } = 7983;
    public bool Metrics { get; init; } = true;
}

/// <summary>
/// The built-in sync schedule of a persistent server. The server parses the cron expression at
/// startup and evaluates it in the system time zone.
/// </summary>
public record ScheduleSettings
{
    public bool Enabled { get; init; } = true;

    // Daily at midnight, matching the container's previous `@daily` cron job.
    public string Cron { get; init; } = "0 0 * * *";
}
