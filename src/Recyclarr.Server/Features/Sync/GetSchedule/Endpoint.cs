using FastEndpoints;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Schedule;

namespace Recyclarr.Server.Features.Sync.GetSchedule;

internal sealed class Endpoint(
    // Lazy: endpoints are constructed while routes are mapped, before settings are migrated.
    Lazy<SyncSchedule> schedule,
    ISyncJobStore jobStore,
    TimeProvider time
) : EndpointWithoutRequest<SyncScheduleResponse>
{
    public override void Configure()
    {
        Get("/sync/schedule");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.WithTags("Sync"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = schedule.Value;
        Response = new SyncScheduleResponse(
            current.Enabled,
            current.Cron,
            current.TimeZone.Id,
            jobStore.LatestScheduledOccurrence(),
            current.NextOccurrence(time.GetUtcNow())
        );
        await Send.ResponseAsync(Response, 200, ct);
    }
}
