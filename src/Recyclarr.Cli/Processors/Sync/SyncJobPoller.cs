using System.Net;
using System.Runtime.CompilerServices;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync;

/// <summary>
/// Polls a sync job until it finishes, yielding every snapshot along the way so callers can render
/// progress as it happens. The job resource answers 202 while the sync is running and 200 once it
/// has reached a terminal state (ADR-011).
/// </summary>
internal static class SyncJobPoller
{
    // The server advertises Retry-After: 1, which is guidance for third-party clients. This one
    // drives a live progress table, so it polls faster than that.
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
            if (response.Error is not null)
            {
                throw response.Error;
            }

            // non-null: a successful response always carries the job resource
            yield return response.Content!;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                yield break;
            }

            await Task.Delay(PollInterval, ct);
        }
    }
}
