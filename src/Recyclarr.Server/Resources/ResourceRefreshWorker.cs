using System.Diagnostics.CodeAnalysis;
using Recyclarr.Server.Hosting;

namespace Recyclarr.Server.Resources;

/// <summary>
/// Keeps a persistent server's provider data current after startup (ADR-031). Each tick asks the
/// coordinator to refresh; the coordinator decides whether any provider is due, so ticks are cheap
/// and a refresh skipped because readers held the data is retried on the next tick.
/// </summary>
/// <remarks>
/// Ephemeral servers serve one CLI command and rely on the startup refresh alone, as they do for
/// the sync schedule.
/// </remarks>
internal sealed class ResourceRefreshWorker(
    ILogger log,
    ResourceCoordinator resources,
    ServerMode mode,
    TimeProvider time
) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (mode == ServerMode.Ephemeral)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    // A failed refresh must not stop later ones.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            await resources.RefreshAsync(RefreshMode.Background, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.Error(e, "Resource refresh failed");
        }
    }
}
