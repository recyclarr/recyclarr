using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Client.V1;
using Refit;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

internal class SyncCommandHandler(
    ILogger log,
    IAnsiConsole console,
    SyncProgressRenderer progressRenderer
)
{
    public async Task<ExitStatus> RunAsync(
        ISyncApi api,
        CreateSyncJobRequest request,
        CancellationToken ct
    )
    {
        var jobId = await CreateJobAsync(api, request, ct);
        if (jobId is null)
        {
            return ExitStatus.Failed;
        }

        var updates = SyncJobPoller.PollAsync(api, jobId.Value, ct);
        var job = await progressRenderer.RenderProgressAsync(updates, ct);

        // Only an outright failure earns a non-zero exit code. A partial sync applied everything
        // it could, which is the outcome the CLI has always reported as success.
        return job.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            ? ExitStatus.Failed
            : ExitStatus.Succeeded;
    }

    // Returns the id of the accepted job, or null when the server refused the request.
    private async Task<Guid?> CreateJobAsync(
        ISyncApi api,
        CreateSyncJobRequest request,
        CancellationToken ct
    )
    {
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

        var message = problem?.Title ?? response.Error?.Message ?? "Unknown error";
        console.MarkupLineInterpolated($"[red]Error:[/] {message}");
        log.Error("Sync request rejected: {Message}", message);

        foreach (var detail in problem?.Errors?.Values.SelectMany(x => x) ?? [])
        {
            console.MarkupLineInterpolated($"  {detail}");
            log.Error("{Detail}", detail);
        }

        if (problem?.UnknownInstances is { Count: > 0 } unknown)
        {
            var available = problem.AvailableInstances ?? [];
            console.MarkupLineInterpolated($"  Unknown instances: {string.Join(", ", unknown)}");
            console.MarkupLineInterpolated(
                $"  Available instances: {string.Join(", ", available)}"
            );
            log.Error(
                "Unknown instances {Unknown}; available instances {Available}",
                unknown,
                available
            );
        }
    }
}
