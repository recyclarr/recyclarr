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
                .ProducesProblemDetails()
                .ProducesProblemDetails(500)
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
        ServerConfigLoadResult loadResult;
        try
        {
            loadResult = configLoader.LoadConfigs(settings);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.Error(e, "Failed to load server configuration for a sync job");
            AddError("Server configuration could not be loaded");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var loadDiagnostics = ConfigLoadDiagnosticsBuilder.Build(loadResult);
        ConfigLoadDiagnosticsLogger.Log(log, loadDiagnostics);

        if (HasServerConfigurationErrors(loadDiagnostics))
        {
            AddError("Server configuration is invalid");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        if (
            loadDiagnostics.MissingConfigFiles.Count > 0
            || loadDiagnostics.UnknownInstances.Count > 0
        )
        {
            AddError("The sync request did not match available configuration");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (loadResult.Configs.Count == 0)
        {
            var status = loadResult.HasAvailableConfigs ? 400 : 500;
            AddError(
                status == 400
                    ? "The sync request did not match available configuration"
                    : "No server configuration is available for synchronization"
            );
            await Send.ErrorsAsync(status, ct);
            return;
        }

        var job = launcher.Launch(settings, loadResult.Configs);

        Response = new CreateSyncJobResponse(job.Id.Value, job.Status.ToString(), job.CreatedAt);
        HttpContext.Response.Headers.Location = $"/api/v1/sync/jobs/{job.Id.Value}";
        await Send.ResponseAsync(Response, 202, ct);
    }

    private static bool HasServerConfigurationErrors(ConfigLoadDiagnostics diagnostics) =>
        diagnostics.ParseFailures.Count > 0
        || diagnostics.InvalidInstances.Count > 0
        || diagnostics.DuplicateInstances.Count > 0
        || diagnostics.SplitInstanceGroups.Count > 0;
}
