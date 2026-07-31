using Recyclarr.Config.Models;
using Recyclarr.Sync.Results;

namespace Recyclarr.Sync;

/// <summary>
/// Coordinates sync runs across configured service instances and returns their terminal results.
/// </summary>
/// <remarks>
/// Expected instance failures are retained and later instances continue. Cancellation propagates;
/// the first unexpected fault ends the run while preserving work that already completed.
/// </remarks>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Processes configurations in order and returns an ordered snapshot of completed work.
    /// </summary>
    Task<SyncRunResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        CancellationToken ct
    );
}
