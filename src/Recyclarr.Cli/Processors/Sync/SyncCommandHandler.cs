using Recyclarr.Cli.Preview;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
using Refit;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncCommandHandler(
    ILogger log,
    IAnsiConsole console,
    SyncProgressRenderer progressRenderer,
    DiagnosticsRenderer diagnosticsRenderer,
    SyncDiagnosticsLogger diagnosticsLogger,
    ConfigDiagnosticsRenderer configDiagnosticsRenderer,
    ConfigDiagnosticsLogger configDiagnosticsLogger,
    PreviewRenderer previewRenderer
)
{
    public async Task<ExitStatus> RunAsync(
        ISyncApi api,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        var jobId = await CreateJobAsync(api, settings, ct);
        if (jobId is null)
        {
            return ExitStatus.Failed;
        }

        var updates = SyncJobPoller.PollAsync(api, jobId.Value, ct);

        // The live table would fight with the preview output for the terminal, so preview runs
        // silently and reports everything once the job is done.
        var job = settings.Preview
            ? await LastAsync(updates, ct)
            : await progressRenderer.RenderProgressAsync(updates, ct);

        if (settings.Preview)
        {
            await RenderPreviewAsync(api, jobId.Value, ct);
        }

        if (job.ConfigDiagnostics is { } configDiagnostics)
        {
            ReportConfigDiagnostics(configDiagnostics);
        }

        diagnosticsLogger.Log(job.Diagnostics);
        diagnosticsRenderer.Render(job.Diagnostics);

        // Only an outright failure earns a non-zero exit code. A partial sync applied everything
        // it could, which is the outcome the CLI has always reported as success.
        return job.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            ? ExitStatus.Failed
            : ExitStatus.Succeeded;
    }

    /// <summary>
    /// Returns the id of the accepted job, or null when the server refused the request. A refusal
    /// is not an error to report on its own: it carries the config diagnostics that explain why
    /// there was nothing to sync.
    /// </summary>
    private async Task<Guid?> CreateJobAsync(
        ISyncApi api,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        var request = new CreateSyncJobRequest
        {
            Service = settings.Service is { } service
                ? Enum.Parse<SupportedServices>(service.ToString())
                : null,
            Instances = [.. settings.Instances ?? []],
            Configs = [.. settings.Configs],
            Preview = settings.Preview,
        };

        using var response = await api.JobsPost(request, ct);
        if (response.IsSuccessful)
        {
            // non-null: a 202 always carries the created job
            return response.Content!.Id;
        }

        await RenderRefusalAsync(response);
        return null;
    }

    private async Task RenderRefusalAsync(IApiResponse<CreateSyncJobResponse> response)
    {
        var problem = response.HasResponseError(out var error)
            ? await error.GetContentAsAsync<CreateSyncJobProblemDetails>()
            : null;

        if (problem?.Diagnostics is { } diagnostics)
        {
            ReportConfigDiagnostics(diagnostics);
            return;
        }

        var message = problem?.Title ?? response.Error?.Message ?? "Unknown error";
        console.MarkupLineInterpolated($"[red]Error:[/] {message}");
        log.Error("Sync request rejected: {Message}", message);
    }

    private void ReportConfigDiagnostics(ConfigDiagnosticsResponse diagnostics)
    {
        configDiagnosticsLogger.Log(diagnostics);
        configDiagnosticsRenderer.Render(diagnostics);
    }

    private async Task RenderPreviewAsync(ISyncApi api, Guid jobId, CancellationToken ct)
    {
        using var response = await api.Results(jobId, ct);
        if (response.Error is not null)
        {
            throw response.Error;
        }

        // non-null: a successful response always carries the results
        previewRenderer.Render(response.Content!);
    }

    private static async Task<GetSyncJobResponse> LastAsync(
        IAsyncEnumerable<GetSyncJobResponse> updates,
        CancellationToken ct
    )
    {
        GetSyncJobResponse? last = null;
        await foreach (var job in updates.WithCancellation(ct))
        {
            last = job;
        }

        return last
            ?? throw new InvalidOperationException("The sync job reported no status at all");
    }
}
