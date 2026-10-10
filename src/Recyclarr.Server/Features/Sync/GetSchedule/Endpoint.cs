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
        Summary(s =>
        {
            s.Summary = "Get the sync schedule";
            s.Description =
                "Returns the built-in schedule that creates sync jobs for every configured "
                + "instance. A scheduled job that comes due while another job is active is "
                + "recorded as `Skipped`. Schedule changes apply after a server restart.";
            s.Responses[200] = "The schedule.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = schedule.Value;
        Response = new SyncScheduleResponse
        {
            Enabled = current.Enabled,
            Cron = current.Cron,
            TimeZone = current.TimeZone.Id,
            PreviousOccurrence = jobStore.LatestScheduledOccurrence(),
            NextOccurrence = current.NextOccurrence(time.GetUtcNow()),
        };
        await Send.ResponseAsync(Response, 200, ct);
    }
}
