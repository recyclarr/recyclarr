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

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b =>
            b.Produces<GetSyncJobResponse>()
                .Produces<GetSyncJobResponse>(202)
                .ProducesProblems(404)
                .WithTags("Sync")
        );
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

        Response = ToResponse(job);

        // 202 while the job runs, 200 once it is terminal (ADR-011).
        if (job.Status.IsTerminal())
        {
            await Send.OkAsync(Response, ct);
            return;
        }

        HttpContext.Response.Headers.RetryAfter = "1";
        await Send.ResponseAsync(Response, 202, ct);
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
                ? new SkippedBecauseResponse(skippedBy.Value)
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
