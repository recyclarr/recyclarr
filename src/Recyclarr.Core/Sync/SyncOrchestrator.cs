using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Notifications;

namespace Recyclarr.Sync;

internal class SyncOrchestrator(
    InstanceScopeFactory instanceScopeFactory,
    INotificationService notify,
    DiagnosticsLogger diagnosticsLogger // activate diagnostic logging subscription
) : ISyncOrchestrator
{
    public async Task<ExitStatus> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        // Injected to activate its diagnostic subscription; no callable API
        _ = diagnosticsLogger;

        var failureDetected = false;

        foreach (var config in configs)
        {
            using var instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);
            var result = await instanceScope.Entry.Process(settings, ct);
            if (result == ExitStatus.Failed)
            {
                failureDetected = true;
            }
        }

        await notify.SendNotification();

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
