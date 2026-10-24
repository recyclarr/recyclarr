using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Observability;

/// <summary>
/// Traces and measures sync job runs. Each run is its own trace; a job created by an API request
/// links to that request's span rather than becoming its child, because the request returns long
/// before the job runs and scheduled jobs have no request at all.
/// </summary>
[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "IMeterFactory owns the meters it creates and disposes them with the host"
)]
internal sealed class SyncJobTelemetry(IMeterFactory meters)
{
    private readonly Histogram<double> _duration = meters
        .Create(ServerTelemetry.SourceName)
        .CreateHistogram<double>(
            "recyclarr.sync.job.duration",
            unit: "s",
            description: "Duration of sync job runs"
        );

    /// <summary>
    /// Starts the span for one job run, or returns null when nothing listens to the Recyclarr
    /// source. A default parent context falls back to the ambient activity, so the caller must run
    /// in a clean execution context for the span to start its own trace.
    /// </summary>
    public static Activity? StartRun(SyncJob job)
    {
        ActivityLink[] links = ActivityContext.TryParse(job.TraceParent, null, out var origin)
            ? [new ActivityLink(origin)]
            : [];

        var activity = ServerTelemetry.Source.StartActivity(
            "sync job",
            ActivityKind.Consumer,
            parentContext: default,
            links: links
        );

        activity?.SetTag("recyclarr.sync.job.id", job.Id.Value.ToString());
        activity?.SetTag("recyclarr.sync.job.trigger", TriggerName(job.Trigger));
        return activity;
    }

    public void RecordRun(SyncJob job, SyncJobStatus finalStatus, TimeSpan elapsed)
    {
        _duration.Record(
            elapsed.TotalSeconds,
            new KeyValuePair<string, object?>(
                "recyclarr.sync.job.trigger",
                TriggerName(job.Trigger)
            ),
            new KeyValuePair<string, object?>(
                "recyclarr.sync.job.status",
                finalStatus.ToString().ToLowerInvariant()
            )
        );
    }

    private static string TriggerName(SyncJobTrigger trigger)
    {
        return trigger.ToString().ToLowerInvariant();
    }
}
