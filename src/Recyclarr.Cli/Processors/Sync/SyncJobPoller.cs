using System.Runtime.CompilerServices;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync;

/// <summary>
/// Polls a sync job until it finishes, yielding every snapshot along the way so callers can render
/// progress as it happens. The job resource always answers 200; its status tells whether the job
/// is still pending or running (ADR-016).
/// </summary>
internal static class SyncJobPoller
{
    /// <summary>
    /// The server advertises Retry-After: 1, which is guidance for third-party clients. This one
    /// drives a live progress table, so it polls faster than that.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    public static async IAsyncEnumerable<GetSyncJobResponse> PollAsync(
        ISyncApi api,
        Guid jobId,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        while (true)
        {
            using var response = await api.JobsGet(jobId, ct);
            var job = response.ContentOrThrow();
            yield return job;

            if (job.Status is not ("Pending" or "Running"))
            {
                yield break;
            }

            await Task.Delay(PollInterval, ct);
        }
    }
}
