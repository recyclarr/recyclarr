using FastEndpoints;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.GetJobResults;

// Sub-resource of the job (ADR-014): the job resource stays small enough to poll, while the
// transaction data a consumer reads exactly once lives behind its own URL.
internal sealed class Endpoint(ISyncJobStore jobStore)
    : Endpoint<GetSyncJobResultsRequest, GetSyncJobResultsResponse>
{
    public override void Configure()
    {
        Get("/sync/jobs/{id}/results");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        // Always 200 for a known job. Fetching before the sync finishes yields a valid partial
        // document rather than an error the client has to special-case.
        Description(b =>
            b.Produces<GetSyncJobResultsResponse>().ProducesProblemDetails(404).WithTags("Sync")
        );
    }

    public override async Task HandleAsync(GetSyncJobResultsRequest req, CancellationToken ct)
    {
        var job = jobStore.Get(new JobId { Value = req.Id });
        if (job is null)
        {
            AddError("Sync job not found");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        Response = new GetSyncJobResultsResponse
        {
            Id = job.Id.Value,
            Instances = job.Results.Select(x => x.Result.ToResponse(x.InstanceName)).ToList(),
        };
        await Send.OkAsync(Response, ct);
    }
}
