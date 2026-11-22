using Microsoft.EntityFrameworkCore;
using Recyclarr.Migration;
using Recyclarr.Server.Diagnostics;
using Recyclarr.Server.Features.Diagnostics.List;
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
    ISyncJobStore jobs,
    SettingsBindResult settings,
    IConfiguration configuration,
    ServerDiagnostics diagnostics,
    ILogger log
) : IHostedLifecycleService
{
    private const string DiagnosticSource = "startup";

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
            "the server writes debug logs to logs/server in the data directory by default, and CLI "
                + "commands show log output on the console with --log",
            "docker-env-removed"
        ),
    ];

    public async Task StartingAsync(CancellationToken ct)
    {
        logger.Reload();

        // Settings and environment variables are read only at startup, so these entries last until
        // the next restart. Logged once the configured outputs exist; settings errors stopped
        // startup earlier.
        var startupDiagnostics = new List<DiagnosticResponse>();
        foreach (
            var warning in settings.Diagnostics.Where(d =>
                d.Severity == SettingsDiagnosticSeverity.Warning
            )
        )
        {
            var message = warning.Describe();
            log.Warning("[DEPRECATED] {Message}", message);
            startupDiagnostics.Add(
                DiagnosticCodes.SettingDeprecated.Create(
                    $"{warning.File}:{warning.Position?.YamlPath ?? warning.Message}",
                    message,
                    new SettingDiagnosticData(warning.File, warning.Position?.Line)
                )
            );
        }

        foreach (var (variable, replacement, anchor) in RemovedVariables)
        {
            if (string.IsNullOrEmpty(configuration[variable]))
            {
                continue;
            }

            var url = $"{UpgradeGuide}#{anchor}";
            log.Warning(
                "[REMOVED] {Variable} is no longer used; {Replacement}. See {Url}",
                variable,
                replacement,
                url
            );
            startupDiagnostics.Add(
                DiagnosticCodes.EnvironmentVariableRemoved.Create(
                    variable,
                    $"{variable} is no longer used; {replacement}. See {url}",
                    new EnvironmentVariableDiagnosticData(variable)
                )
            );
        }

        diagnostics.Replace(DiagnosticSource, startupDiagnostics);

        if (logger.ActiveLogFiles.Count > 0)
        {
            logJanitor.DeleteOldestLogFiles(logger.ActiveLogFiles);
        }

        migrations.RunAll();
        await resources.RefreshAsync(RefreshMode.Startup, ct);

        await using (var db = await serverDb.CreateDbContextAsync(ct))
        {
            await db.Database.MigrateAsync(ct);
        }

        // The host runs every StartingAsync before any StartAsync, so the job queue has not started
        // and any active job was cut off by the previous shutdown.
        jobs.InterruptActive();
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken ct) => Task.CompletedTask;
}
