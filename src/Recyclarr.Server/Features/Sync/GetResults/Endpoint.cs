using FastEndpoints;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Results;

namespace Recyclarr.Server.Features.Sync.GetResults;

internal sealed class Endpoint(ILogger log, ISyncJobStore jobStore)
    : Endpoint<GetSyncJobResultsRequest, SyncJobResultsResponse>
{
    public override void Configure()
    {
        Get("/sync/jobs/{id}/results");
        Version(1);
        AllowAnonymous();

        Description(b =>
            b.Produces<SyncJobResultsResponse>()
                .ProducesProblemDetails(409)
                .ProducesProblemDetails(404)
                .ProducesProblemDetails(500)
                .WithTags("Sync")
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

        if (!job.Status.IsTerminal())
        {
            AddError("Sync job has not completed");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        try
        {
            await Send.OkAsync(SyncJobResultsResponseMapper.Map(job), ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.Error(e, "Failed to return results for sync job {JobId}", job.Id);
            AddError("Sync job results could not be returned");
            await Send.ErrorsAsync(500, ct);
        }
    }
}
