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

        Description(b => b.WithTags("Sync"));
        Summary(s =>
        {
            s.Summary = "List sync jobs";
            s.Description =
                "Lists the sync jobs the server keeps, oldest first. To check whether a sync is "
                + "active, filter by status `Running` or `Pending`.";
            s.Responses[200] = "The matching jobs.";
            s.Responses[400] = "A filter value is not one of the listed values.";
        });
    }

    public override async Task HandleAsync(ListSyncJobsRequest req, CancellationToken ct)
    {
        // Validated in the Validator; safe to parse without a fallback here.
        SyncJobStatus? statusFilter = req.Status is null
            ? null
            : Enum.Parse<SyncJobStatus>(req.Status, ignoreCase: true);
        SyncJobTrigger? triggerFilter = req.Trigger is null
            ? null
            : Enum.Parse<SyncJobTrigger>(req.Trigger, ignoreCase: true);

        var jobs = jobStore
            .GetAll(statusFilter, triggerFilter)
            .Select(j => new SyncJobSummaryResponse
            {
                Id = j.Id.Value,
                Status = j.Status.ToString(),
                Trigger = j.Trigger.ToString(),
                CreatedAt = j.CreatedAt,
            })
            .ToList();

        Response = new ListSyncJobsResponse(jobs);
        await Send.ResponseAsync(Response, 200, ct);
    }
}
