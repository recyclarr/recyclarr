using Microsoft.EntityFrameworkCore;
using Recyclarr.Migration;
using Recyclarr.Server.Logging;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync;
using Recyclarr.Settings.Configuration;

namespace Recyclarr.Server.Hosting;

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
    ISyncJobStore jobs,
    SettingsBindResult settings,
    IConfiguration configuration,
    ILogger log
) : IHostedLifecycleService
{
    private const string UpgradeGuide = "https://recyclarr.dev/guide/upgrade-guide/v9.0/";

    // Recyclarr 8 Docker variables that Recyclarr 9 ignores. Containers upgraded without reading
    // the upgrade guide often still set them, so each one gets a pointer to its replacement.
    private static readonly (
        string Variable,
        string Replacement,
        string Anchor
    )[] RemovedVariables =
    [
        ("CRON_SCHEDULE", "set `server.schedule.cron` in settings.yml instead", "cron-schedule"),
        (
            "RECYCLARR_CREATE_CONFIG",
            "create configuration files in the configuration directory yourself",
            "docker-env-removed"
        ),
        (
            "RECYCLARR_LOG_LEVEL",
            "pass --log to CLI commands to show log output on the console",
            "docker-env-removed"
        ),
    ];

    public async Task StartingAsync(CancellationToken ct)
    {
        logger.Reload();

        // Logged once the configured outputs exist; settings errors stopped startup earlier.
        foreach (
            var warning in settings.Diagnostics.Where(d =>
                d.Severity == SettingsDiagnosticSeverity.Warning
            )
        )
        {
            log.Warning("[DEPRECATED] {Message}", warning.Describe());
        }

        foreach (var (variable, replacement, anchor) in RemovedVariables)
        {
            if (string.IsNullOrEmpty(configuration[variable]))
            {
                continue;
            }

            log.Warning(
                "[REMOVED] {Variable} is no longer used; {Replacement}. See {Url}",
                variable,
                replacement,
                $"{UpgradeGuide}#{anchor}"
            );
        }

        if (logger.ActiveLogFiles.Count > 0)
        {
            logJanitor.DeleteOldestLogFiles(logger.ActiveLogFiles);
        }

        migrations.RunAll();
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
