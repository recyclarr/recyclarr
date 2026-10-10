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
                .ProducesProblems(400, 404, 409, 500)
                .WithTags("Sync")
        );
        Summary(s =>
        {
            s.Summary = "Get sync job results";
            s.Description =
                "Returns the changes and outcomes of a finished job for each instance that "
                + "finished. For a preview job, the changes were computed but not applied.";
            s.Responses[200] = "The job results.";
            s.Responses[400] = "The ID is not a GUID.";
            s.Responses[404] = "No job has this ID.";
            s.Responses[409] = "The job is still active, or it was skipped and has no results.";
            s.Responses[500] =
                "The server could not build the results. The server log has details.";
        });
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

        if (job.Status == SyncJobStatus.Skipped)
        {
            AddError("Sync job was skipped and has no results");
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
