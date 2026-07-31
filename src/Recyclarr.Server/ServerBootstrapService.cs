using Recyclarr.Migration;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Server;

/// <summary>
/// One-time bootstrap of on-disk state: schema migrations first, then resource providers (custom
/// formats, quality profiles, and friends). Both must be complete before any request is served,
/// which is why this runs in <c>StartingAsync</c>: the generic host finishes that phase for every
/// <see cref="IHostedLifecycleService"/> before starting any <see cref="IHostedService"/>,
/// including the one that binds Kestrel.
/// </summary>
internal sealed class ServerBootstrapService(
    IMigrationExecutor migrations,
    ProviderInitializationFactory providers,
    ServerLogJanitor logJanitor,
    ServerLogger logger
) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken ct)
    {
        if (logger.ActiveLogFile is { } activeLogFile)
        {
            logJanitor.DeleteOldestLogFiles(activeLogFile);
        }

        migrations.PerformAllMigrationSteps();
        await providers.InitializeProvidersAsync(progress: null, ct);
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken ct) => Task.CompletedTask;
}
