using Recyclarr.Config;
using Recyclarr.Config.Models;

namespace Recyclarr.Sync;

internal class SyncOrchestrator(InstanceScopeFactory instanceScopeFactory) : ISyncOrchestrator
{
    public async Task<ExitStatus> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
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

        return failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
