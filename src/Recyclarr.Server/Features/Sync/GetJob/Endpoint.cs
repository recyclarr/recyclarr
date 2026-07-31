using FastEndpoints;
using Recyclarr.Server.Sync;
using Recyclarr.Sync.Progress;

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

        // 200 once the job reaches a terminal state, 202 while it is still running.
        Description(b =>
            b.Produces<GetSyncJobResponse>()
                .Produces<GetSyncJobResponse>(202)
                .ProducesProblemDetails(404)
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

        var statusCode = job.Status.IsTerminal() ? 200 : 202;
        if (statusCode == 202)
        {
            HttpContext.Response.Headers.RetryAfter = "1";
        }

        await Send.ResponseAsync(Response, statusCode, ct);
    }

    private static GetSyncJobResponse ToResponse(SyncJob job)
    {
        return new GetSyncJobResponse
        {
            Id = job.Id.Value,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            Service = job.Request.Service,
            Instances = job.Request.Instances,
            Preview = job.Request.Preview,
            Progress = job.Progress.Instances.Select(ToInstanceResponse).ToList(),
            Diagnostics = job
                .Diagnostics.Select(d => new DiagnosticEventResponse(d.Level.ToString(), d.Message)
                {
                    Instance = d.Instance,
                })
                .ToList(),
            ConfigDiagnostics = job.ConfigDiagnostics?.ToResponse(),
        };
    }

    private static InstanceSnapshotResponse ToInstanceResponse(InstanceSnapshot instance)
    {
        return new InstanceSnapshotResponse(
            instance.Name,
            instance.Status.ToString(),
            instance
                .Pipelines.Select(kvp => new PipelineSnapshotResponse(
                    kvp.Key.ToString(),
                    kvp.Value.Status.ToString()
                )
                {
                    Count = kvp.Value.Count,
                    Changes = kvp.Value.Changes is null
                        ? null
                        : new PipelineItemChangesResponse(
                            kvp.Value.Changes.Created,
                            kvp.Value.Changes.Updated,
                            kvp.Value.Changes.Deleted
                        ),
                })
                .ToList()
        );
    }
}
