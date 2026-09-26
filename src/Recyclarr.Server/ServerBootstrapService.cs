using Recyclarr.Migration;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server;

/// <summary>
/// One-time bootstrap of on-disk state: schema migrations first, then resource providers (custom
/// formats, quality profiles, and friends), then configuration, whose template includes need the
/// providers. All must be complete before any request is served,
/// which is why this runs in <c>StartingAsync</c>: the generic host finishes that phase for every
/// <see cref="IHostedLifecycleService"/> before starting any <see cref="IHostedService"/>,
/// including the one that binds Kestrel.
/// </summary>
internal sealed class ServerBootstrapService(
    IMigrationExecutor migrations,
    ProviderInitializationFactory providers,
    ServerLogJanitor logJanitor,
    ServerLogger logger,
    Lazy<ServerConfiguration> configuration
) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken ct)
    {
        if (logger.ActiveLogFiles.Count > 0)
        {
            logJanitor.DeleteOldestLogFiles(logger.ActiveLogFiles);
        }

        migrations.PerformAllMigrationSteps();
        await providers.InitializeProvidersAsync(progress: null, ct);
        // Force the configuration snapshot to load now, so invalid configuration fails startup
        // and deprecations are logged before any request is served.
        _ = configuration.Value;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken ct) => Task.CompletedTask;
}
