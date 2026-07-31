namespace Recyclarr.Server.Sync;

// Process-lifetime, in-memory job storage with ring-buffer eviction. Never evicts non-terminal
// jobs; caps terminal jobs at MaxTerminalJobs, evicting the oldest first. Distinct from Core's
// ISyncRunStorage, which holds a single run's computed results and dies with that run's scope.
internal sealed class InMemorySyncJobStore : ISyncJobStore
{
    private const int MaxTerminalJobs = 50;

    private readonly Lock _gate = new();
    private readonly Dictionary<JobId, SyncJob> _jobs = [];
    private readonly List<JobId> _order = [];

    public SyncJob Create(ServerSyncSettings request)
    {
        var job = new SyncJob(JobId.New(), request, DateTimeOffset.UtcNow);

        lock (_gate)
        {
            _jobs[job.Id] = job;
            _order.Add(job.Id);
            EvictExcessTerminalJobs();
        }

        return job;
    }

    public SyncJob? Get(JobId id)
    {
        lock (_gate)
        {
            return _jobs.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<SyncJob> GetAll(SyncJobStatus? statusFilter)
    {
        lock (_gate)
        {
            return _order
                .Select(id => _jobs[id])
                .Where(j => statusFilter is null || j.Status == statusFilter)
                .ToList();
        }
    }

    public void Update(JobId id, Action<SyncJob> mutate)
    {
        lock (_gate)
        {
            if (!_jobs.TryGetValue(id, out var job))
            {
                return;
            }

            mutate(job);

            if (job.Status.IsTerminal())
            {
                EvictExcessTerminalJobs();
            }
        }
    }

    // Caller must hold _gate.
    private void EvictExcessTerminalJobs()
    {
        var terminalIds = _order.Where(id => _jobs[id].Status.IsTerminal()).ToList();
        var excess = terminalIds.Count - MaxTerminalJobs;

        for (var i = 0; i < excess; i++)
        {
            _jobs.Remove(terminalIds[i]);
            _order.Remove(terminalIds[i]);
        }
    }
}
