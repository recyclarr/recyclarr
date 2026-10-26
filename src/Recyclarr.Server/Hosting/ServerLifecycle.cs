using System.ServiceProcess;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Recyclarr.Logging;
using Recyclarr.Server.Api.Auth;
using Recyclarr.Server.Observability;

namespace Recyclarr.Server.Hosting;

internal static class ServerLifecycle
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Starts the server, reports readiness, and waits for it to stop. Returns the process exit
        /// code: 1 when startup fails or a background service faulted, otherwise 0.
        /// </summary>
        public async Task<int> RunServerAsync(ServerLaunchOptions launch)
        {
            if (!await TryStartAsync(app, launch.IsDocumentGeneration))
            {
                return 1;
            }

            AnnounceReady(app, launch.IsDocumentGeneration);

            var failed = await WaitForStopAsync(app);
            await app.StopAsync();
            return failed ? 1 : 0;
        }
    }

    private static async Task<bool> TryStartAsync(WebApplication app, bool isDocumentGeneration)
    {
        try
        {
            if (!isDocumentGeneration)
            {
                // First, so nothing touches directories another running server owns.
                app.Services.GetRequiredService<DataDirectoryLock>().Acquire();

                // Fail closed: without a key the server would serve its API to anyone (ADR-027).
                app.Services.GetRequiredService<ServerApiKey>();
                app.Services.GetRequiredService<SettingsEndpointSetup>().EnsureValidPorts();
            }

            await app.StartAsync();
            return true;
        }
        catch (Exception e)
            when ((e as FatalException ?? e.FindInnerException<FatalException>()) is { } fatal)
        {
            // The cause is already logged; startup fails without a READY line so a parent CLI sees
            // the child exit instead of a server that cannot sync (ADR-019).
            app.Services.GetRequiredService<ILogger>()
                .Fatal("Server startup failed: {Message}", fatal.Message);
            return false;
        }
    }

    // Uses bound addresses, not configured ones: the ephemeral launcher requests port 0, so only
    // the bound address carries the real port.
    private static void AnnounceReady(WebApplication app, bool isDocumentGeneration)
    {
        var addresses =
            app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()
                ?.Addresses
            ?? [];

        int? managementPort = null;
        if (!isDocumentGeneration)
        {
            var management = app.Services.GetRequiredService<ManagementPort>();
            managementPort = management.Number;
            LogListenAddresses(app.Services.GetRequiredService<ILogger>(), addresses, management);
        }

        var apiPort = addresses
            .Select(a => new Uri(a).Port)
            .FirstOrDefault(p => p != managementPort, -1);
        app.Services.GetRequiredService<IReadySignal>().Ready(apiPort);
    }

    private static void LogListenAddresses(
        ILogger log,
        IEnumerable<string> addresses,
        ManagementPort management
    )
    {
        foreach (var address in addresses)
        {
            if (new Uri(address).Port != management.Number)
            {
                log.Information("API listening on {Address}", address);
                continue;
            }

            var serves = management.ServesMetrics ? "health, metrics" : "health";
            log.Information("Management ({Serves}) listening on {Address}", serves, address);
        }
    }

    // A faulted background service stops the host the same way a stop request does. Exiting with
    // 0 after one would hide the failure from systemd (Restart=on-failure) and the Windows SCM
    // recovery actions, so the stop is reported as a failure before the host lifetime announces
    // it.
    private static async Task<bool> WaitForStopAsync(WebApplication app)
    {
        var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        app.Lifetime.ApplicationStopping.Register(() => stopping.TrySetResult());
        await stopping.Task;

        var failed = app
            .Services.GetServices<IHostedService>()
            .OfType<BackgroundService>()
            .Any(s => s.ExecuteTask is { IsFaulted: true });

        if (
            failed
            && OperatingSystem.IsWindows()
            && app.Services.GetRequiredService<IHostLifetime>() is ServiceBase windowsService
        )
        {
            // The SCM reads the exit code from the SERVICE_STOPPED status, not from the process.
            const int errorExceptionInService = 1064;
            windowsService.ExitCode = errorExceptionInService;
        }

        return failed;
    }
}
