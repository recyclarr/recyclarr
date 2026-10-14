using FastEndpoints;
using Recyclarr.Server.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.CreateJob;

internal sealed class Endpoint(
    ServerConfigurationStore configurationStore,
    ISyncJobStore store,
    // Lazy: endpoints are constructed while routes are mapped, including during build-time OpenAPI
    // generation, where TickerQ (the launcher's queue) is not registered.
    Lazy<SyncJobLauncher> launcher
) : Endpoint<CreateSyncJobRequest, CreateSyncJobResponse>
{
    public override void Configure()
    {
        Post("/sync/jobs");
        Version(1);

        Description(b =>
            b.ClearDefaultProduces()
                .Produces<CreateSyncJobResponse>(202)
                .Produces<CreateSyncJobProblemDetails>(400, "application/problem+json")
                .ProducesProblems(409)
                .WithTags("Sync")
        );
        Summary(s =>
        {
            s.Summary = "Start a sync job";
            s.Description =
                "Creates a sync job for the selected instances and queues it. Jobs run one at "
                + "a time, oldest first, so a new job stays `Pending` while another job runs. "
                + "Poll the URL in the `Location` header to follow the job. The server reads "
                + "instance configuration at startup, so configuration changes apply after a "
                + "restart.";
            s.ExampleRequest = new CreateSyncJobRequest
            {
                Service = SupportedServices.Radarr,
                Instances = ["radarr-4k"],
                Preview = true,
            };
            s.Responses[202] = "The job was created and queued.";
            s.Responses[400] =
                "The request is not valid, names instances that are not configured, or selects "
                + "no configured instance.";
            s.Responses[409] = "No instances are configured on the server.";
        });
    }

    public override async Task HandleAsync(CreateSyncJobRequest req, CancellationToken ct)
    {
        var configuration = configurationStore.Current;
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
            await SendUnknownInstancesAsync(unknown, configuration.InstanceNames);
            return;
        }

        var selected = configuration.Select(settings.Service, settings.Instances);
        if (selected.Count == 0)
        {
            AddError("The sync request did not match any configured instance");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var job = store.Create(settings, [.. selected.Select(x => x.InstanceName)]);
        await launcher.Value.LaunchAsync(job.Id);

        Response = new CreateSyncJobResponse
        {
            Id = job.Id.Value,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
        };
        HttpContext.Response.Headers.Location = $"/api/v1/sync/jobs/{job.Id.Value}";
        await Send.ResponseAsync(Response, 202, ct);
    }

    private async Task SendUnknownInstancesAsync(
        IReadOnlyList<string> unknown,
        IReadOnlyList<string> available
    )
    {
        var problem = ErrorResponses.Apply(
            new CreateSyncJobProblemDetails
            {
                Title = "The sync request names instances that are not configured",
                UnknownInstances = unknown,
                AvailableInstances = available,
            },
            HttpContext,
            400
        );

        await Send.ResultAsync(
            TypedResults.Json(problem, statusCode: 400, contentType: "application/problem+json")
        );
    }
}
