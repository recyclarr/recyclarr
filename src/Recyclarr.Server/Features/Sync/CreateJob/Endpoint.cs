using FastEndpoints;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.CreateJob;

internal sealed class Endpoint(ServerConfiguration configuration, SyncJobLauncher launcher)
    : Endpoint<CreateSyncJobRequest, CreateSyncJobResponse>
{
    public override void Configure()
    {
        Post("/sync/jobs");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b =>
            b.ClearDefaultProduces()
                .Produces<CreateSyncJobResponse>(202)
                .Produces<CreateSyncJobProblemDetails>(400, "application/problem+json")
                .ProducesProblems(409)
                .WithTags("Sync")
        );
    }

    public override async Task HandleAsync(CreateSyncJobRequest req, CancellationToken ct)
    {
        if (configuration.Instances.Count == 0)
        {
            AddError("No instances are configured on the server");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var settings = new ServerSyncSettings(req.Service, req.Instances ?? [], req.Preview);
        var unknown = configuration.FindUnknown(settings.Instances);
        if (unknown.Count > 0)
        {
            await SendUnknownInstancesAsync(unknown);
            return;
        }

        var selected = configuration.Select(settings.Service, settings.Instances);
        if (selected.Count == 0)
        {
            AddError("The sync request did not match any configured instance");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var job = launcher.Launch(settings, selected);

        Response = new CreateSyncJobResponse(job.Id.Value, job.Status.ToString(), job.CreatedAt);
        HttpContext.Response.Headers.Location = $"/api/v1/sync/jobs/{job.Id.Value}";
        await Send.ResponseAsync(Response, 202, ct);
    }

    private async Task SendUnknownInstancesAsync(IReadOnlyList<string> unknown)
    {
        var problem = ErrorResponses.Apply(
            new CreateSyncJobProblemDetails
            {
                Title = "The sync request names instances that are not configured",
                UnknownInstances = unknown,
                AvailableInstances = configuration.InstanceNames,
            },
            HttpContext,
            400
        );

        await Send.ResultAsync(
            TypedResults.Json(problem, statusCode: 400, contentType: "application/problem+json")
        );
    }
}
