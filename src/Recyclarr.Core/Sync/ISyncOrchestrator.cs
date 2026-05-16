using Recyclarr.Config.Models;

namespace Recyclarr.Sync;

public interface ISyncOrchestrator
{
    Task<SyncJobResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    );
}
