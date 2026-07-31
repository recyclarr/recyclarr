using FastEndpoints;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.ListJobs;

internal sealed class Endpoint(ISyncJobStore jobStore)
    : Endpoint<ListSyncJobsRequest, ListSyncJobsResponse>
{
    public override void Configure()
    {
        Get("/sync/jobs");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.WithTags("Sync"));
    }

    public override async Task HandleAsync(ListSyncJobsRequest req, CancellationToken ct)
    {
        // Validated in the Validator; safe to parse without a fallback here.
        SyncJobStatus? statusFilter = req.Status is null
            ? null
            : Enum.Parse<SyncJobStatus>(req.Status, ignoreCase: true);

        var jobs = jobStore
            .GetAll(statusFilter)
            .Select(j => new SyncJobSummaryResponse(j.Id.Value, j.Status.ToString(), j.CreatedAt))
            .ToList();

        Response = new ListSyncJobsResponse(jobs);
        await Send.ResponseAsync(Response, 200, ct);
    }
}
