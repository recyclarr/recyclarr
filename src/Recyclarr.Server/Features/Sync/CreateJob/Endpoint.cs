using FastEndpoints;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.CreateJob;

internal sealed class Endpoint(
    ILogger log,
    ServerConfigLoader configLoader,
    SyncJobLauncher launcher
) : Endpoint<CreateSyncJobRequest, CreateSyncJobResponse>
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
                .WithTags("Sync")
        );
    }

    public override async Task HandleAsync(CreateSyncJobRequest req, CancellationToken ct)
    {
        var settings = new ServerSyncSettings(
            req.Service,
            req.Instances ?? [],
            req.Preview,
            req.Configs ?? []
        );
        var loadResult = configLoader.LoadConfigs(settings);
        var loadDiagnostics = ConfigLoadDiagnosticsBuilder.Build(loadResult);
        ConfigLoadDiagnosticsLogger.Log(log, loadDiagnostics);

        if (loadResult.Configs.Count == 0)
        {
            // No configs matched the request; nothing to sync. The client gets the structured
            // diagnostics directly instead of us rendering prose into a validation failure.
            var problem = new CreateSyncJobProblemDetails
            {
                Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
                Title =
                    loadDiagnostics.MissingConfigFiles.Count > 0
                        ? "Config files not found"
                        : "No instances to sync",
                Status = 400,
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier,
                Diagnostics = loadDiagnostics.ToResponse(),
            };
            HttpContext.MarkResponseStart();
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: ct
            );
            return;
        }

        var job = launcher.Launch(settings, loadDiagnostics, loadResult.Configs);

        Response = new CreateSyncJobResponse(job.Id.Value, job.Status.ToString(), job.CreatedAt);
        HttpContext.Response.Headers.Location = $"/api/v1/sync/jobs/{job.Id.Value}";
        await Send.ResponseAsync(Response, 202, ct);
    }
}
