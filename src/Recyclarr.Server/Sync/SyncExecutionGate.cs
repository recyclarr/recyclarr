namespace Recyclarr.Server.Sync;

/// <summary>
/// Lets one sync run at a time and admits waiting jobs oldest first (by creation time).
/// </summary>
/// <remarks>
/// TickerQ runs due jobs on parallel workers and its concurrency semaphores do not preserve order,
/// so ordering is enforced here. Ceiling: each waiting job holds a TickerQ worker; when the number
/// of waiting jobs reaches TickerQ's worker count, later jobs start late (still in order).
/// </remarks>
internal sealed class SyncExecutionGate
{
    private readonly Lock _lock = new();
    private readonly List<Waiter> _waiting = [];
    private bool _occupied;

    /// <summary>
    /// Completes once <paramref name="job"/> may run. Dispose the result when the run ends so the
    /// next job is admitted. Cancellation withdraws a job that is still waiting.
    /// </summary>
    public async Task<IDisposable> EnterAsync(SyncJob job, CancellationToken ct)
    {
        var waiter = new Waiter(job.CreatedAt, job.Id.Value);
        lock (_lock)
        {
            _waiting.Add(waiter);
            AdmitNext();
        }

        await using (ct.Register(() => Withdraw(waiter, ct)))
        {
            await waiter.Admission.Task;
        }

        return new Admission(this);
    }

    private void Withdraw(Waiter waiter, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_waiting.Remove(waiter))
            {
                waiter.Admission.TrySetCanceled(ct);
            }
        }
    }

    private void Exit()
    {
        lock (_lock)
        {
            _occupied = false;
            AdmitNext();
        }
    }

    // Caller holds _lock.
    private void AdmitNext()
    {
        if (_occupied || _waiting.Count == 0)
        {
            return;
        }

        var next = _waiting.MinBy(x => (x.CreatedAt, x.JobId))!;
        _waiting.Remove(next);
        _occupied = true;
        next.Admission.TrySetResult();
    }

    private sealed class Waiter(DateTimeOffset createdAt, Guid jobId)
    {
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public Guid JobId { get; } = jobId;
        public TaskCompletionSource Admission { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Admission(SyncExecutionGate gate) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                gate.Exit();
            }
        }
    }
}
