using Recyclarr.Config.Models;
using Recyclarr.Sync.Results;

namespace Recyclarr.Sync;

/// <summary>
/// Coordinates sync runs across configured service instances and returns their terminal results.
/// </summary>
/// <remarks>
/// Expected failures and unexpected faults are retained on their owning instance, and later
/// instances continue. Cancellation propagates and stops the run.
/// </remarks>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Processes configurations in order and returns an ordered snapshot of completed work.
    /// </summary>
    Task<SyncRunResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        IInstanceSyncProgress progress,
        CancellationToken ct
    );
}
