using FastEndpoints;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;

namespace Recyclarr.Server.Features.Sync.GetJob;

internal sealed class Endpoint(ISyncJobStore jobStore)
    : Endpoint<GetSyncJobRequest, GetSyncJobResponse>
{
    public override void Configure()
    {
        Get("/sync/jobs/{id}");
        Version(1);

        Description(b =>
            b.Produces<GetSyncJobResponse>().ProducesProblems(400, 404).WithTags("Sync")
        );
        Summary(s =>
        {
            s.Summary = "Get a sync job";
            s.Description =
                "Returns the job's status and the progress of each instance. While the job is "
                + "`Pending` or `Running`, the response has a `Retry-After` header with the "
                + "number of seconds to wait before the next poll. Progress accumulates, so a "
                + "missed poll loses nothing.";
            s.Responses[200] = "The job, in any status.";
            s.Responses[400] = "The ID is not a GUID.";
            s.Responses[404] = "No job has this ID.";
        });
    }

    public override async Task HandleAsync(GetSyncJobRequest req, CancellationToken ct)
    {
        var job = jobStore.Get(new JobId { Value = req.Id });
        if (job is null)
        {
            AddError("Sync job not found");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        // The job status in the body tells clients whether to keep polling (ADR-016).
        if (!job.Status.IsTerminal())
        {
            HttpContext.Response.Headers.RetryAfter = "1";
        }

        await Send.OkAsync(ToResponse(job), ct);
    }

    private static GetSyncJobResponse ToResponse(SyncJob job)
    {
        return new GetSyncJobResponse
        {
            Id = job.Id.Value,
            Status = job.Status.ToString(),
            Trigger = job.Trigger.ToString(),
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            ScheduledFor = job.ScheduledFor,
            SkippedBecause = job.SkippedBy is { } skippedBy
                ? new SkippedBecauseResponse { JobId = skippedBy.Value }
                : null,
            Service = job.Request.Service,
            Instances = job.Request.Instances,
            Preview = job.Request.Preview,
            Progress = job.Progress.Instances.Select(ToInstanceResponse).ToList(),
        };
    }

    private static InstanceSnapshotResponse ToInstanceResponse(InstanceSnapshot instance)
    {
        return new InstanceSnapshotResponse(instance.Name, MapStatus(instance.Status));
    }

    private static InstanceProgressStatusResponse MapStatus(InstanceProgressStatus status) =>
        status switch
        {
            InstanceProgressStatus.Pending => InstanceProgressStatusResponse.Pending,
            InstanceProgressStatus.Running => InstanceProgressStatusResponse.Running,
            InstanceProgressStatus.Succeeded => InstanceProgressStatusResponse.Succeeded,
            InstanceProgressStatus.Partial => InstanceProgressStatusResponse.Partial,
            InstanceProgressStatus.Failed => InstanceProgressStatusResponse.Failed,
            InstanceProgressStatus.Interrupted => InstanceProgressStatusResponse.Interrupted,
            InstanceProgressStatus.NotRun => InstanceProgressStatusResponse.NotRun,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
