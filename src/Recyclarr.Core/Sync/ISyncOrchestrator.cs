using Recyclarr.Config.Models;

namespace Recyclarr.Sync;

public interface ISyncOrchestrator
{
    Task<ExitStatus> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    );
}
