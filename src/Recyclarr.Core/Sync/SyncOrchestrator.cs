using Recyclarr.Config;
using Recyclarr.Config.Models;

namespace Recyclarr.Sync;

internal class SyncOrchestrator(
    InstanceScopeFactory instanceScopeFactory,
    DiagnosticsLogger diagnosticsLogger // activate diagnostic logging subscription
) : ISyncOrchestrator
{
    public async Task<SyncJobResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    )
    {
        // Injected to activate its diagnostic subscription; no callable API
        _ = diagnosticsLogger;

        var jobId = JobId.New();
        var failureDetected = false;

        foreach (var config in configs)
        {
            using var instanceScope = instanceScopeFactory.Start<InstanceSyncProcessor>(config);
            var result = await instanceScope.Entry.Process(settings, jobId, ct);
            if (result == ExitStatus.Failed)
            {
                failureDetected = true;
            }
        }

        var status = failureDetected ? ExitStatus.Failed : ExitStatus.Succeeded;
        return new SyncJobResult(jobId, status);
    }
}
