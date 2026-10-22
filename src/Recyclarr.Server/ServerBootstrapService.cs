using Microsoft.EntityFrameworkCore;
using Recyclarr.Migration;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server;

/// <summary>
/// One-time bootstrap of on-disk state: schema migrations first, then the first resource refresh
/// (provider data plus the configuration resolved against it), then the server database. All must
/// be complete before any request is served, which is why this runs in <c>StartingAsync</c>: the
/// generic host finishes that phase for every <see cref="IHostedLifecycleService"/> before starting
/// any <see cref="IHostedService"/>, including the one that binds Kestrel.
/// </summary>
internal sealed class ServerBootstrapService(
    IMigrationExecutor migrations,
    ResourceCoordinator resources,
    ServerLogJanitor logJanitor,
    ServerLogger logger,
    IDbContextFactory<ServerDbContext> serverDb,
    IDbContextFactory<TickerQueueDbContext> tickerQueueDb,
    ISyncJobStore jobs
) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken ct)
    {
        logger.Reload();

        if (logger.ActiveLogFiles.Count > 0)
        {
            logJanitor.DeleteOldestLogFiles(logger.ActiveLogFiles);
        }

        migrations.PerformAllMigrationSteps();
        await resources.RefreshAsync(RefreshMode.Startup, ct);

        // TickerQ's hosted services read their tables as soon as they start. The host runs every
        // StartingAsync before any StartAsync, so its tables exist by then.
        await MigrateAsync(serverDb, ct);
        await MigrateAsync(tickerQueueDb, ct);

        // Nothing can be running yet, so any active job was cut off by the previous shutdown.
        jobs.InterruptActive();
    }

    private static async Task MigrateAsync<TContext>(
        IDbContextFactory<TContext> factory,
        CancellationToken ct
    )
        where TContext : DbContext
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.MigrateAsync(ct);
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken ct) => Task.CompletedTask;
}
