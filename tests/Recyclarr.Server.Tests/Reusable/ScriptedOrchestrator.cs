using System.Collections.Concurrent;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Tests.Reusable;

/// <summary>
/// Sync engine stub for tests that run jobs through the real queue. The first run follows
/// <see cref="Script"/>; later runs succeed with no changes.
/// </summary>
/// <remarks>
/// The queue runs one job at a time and finishes a job (status, results, span) before it starts
/// the next. Awaiting <see cref="Started"/> for a later run therefore proves every earlier job has
/// finished, without polling on real time.
/// </remarks>
internal sealed class ScriptedOrchestrator : ISyncOrchestrator
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _started = [];
    private Func<IInstanceSyncProgress, SyncRunResult>? _script;
    private int _runs;

    public Func<IInstanceSyncProgress, SyncRunResult> Script
    {
        set => _script = value;
    }

    /// <summary>Completes when the given run (1-based) starts.</summary>
    public Task Started(int run) => Signal(run).Task.WaitAsync(TimeSpan.FromSeconds(10));

    public Task<SyncRunResult> RunAsync(
        IReadOnlyList<IServiceConfiguration> configs,
        ISyncSettings settings,
        IInstanceSyncProgress progress,
        CancellationToken ct
    )
    {
        Signal(Interlocked.Increment(ref _runs)).TrySetResult();
        var script = Interlocked.Exchange(ref _script, null);
        return Task.FromResult(script?.Invoke(progress) ?? new SyncRunResult([]));
    }

    private TaskCompletionSource Signal(int run) =>
        _started.GetOrAdd(
            run,
            _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        );
}
