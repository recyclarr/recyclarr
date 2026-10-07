using Microsoft.EntityFrameworkCore;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;
using TickerQ.Utilities.Entities;

namespace Recyclarr.Server.Persistence;

internal sealed class DatabaseSyncJobStore(
    IDbContextFactory<ServerDbContext> dbFactory,
    IDbContextFactory<TickerQueueDbContext> tickerQueueFactory,
    TimeProvider time
) : ISyncJobStore
{
    private const int MaxTerminalJobs = 50;

    // Serializes read-modify-write cycles; the database is local to this process.
    private readonly Lock _gate = new();

    public SyncJob Create(ServerSyncSettings request, IReadOnlyList<string> instanceNames)
    {
        lock (_gate)
        {
            using var db = dbFactory.CreateDbContext();
            var job = NewJob(
                request,
                instanceNames,
                SyncJobTrigger.Manual,
                scheduledFor: null,
                skippedBy: null
            );
            db.SyncJobs.Add(SyncJobRecordMapper.ToRecord(job));
            db.SaveChanges();
            return job;
        }
    }

    public SyncJob CreateScheduled(
        ServerSyncSettings request,
        IReadOnlyList<string> instanceNames,
        DateTimeOffset scheduledFor
    )
    {
        lock (_gate)
        {
            using var db = dbFactory.CreateDbContext();
            var active = db
                .SyncJobs.Where(x =>
                    x.Status == SyncJobStatus.Pending || x.Status == SyncJobStatus.Running
                )
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();

            var job = NewJob(
                request,
                instanceNames,
                SyncJobTrigger.Scheduled,
                scheduledFor,
                skippedBy: active is { } activeId ? new JobId { Value = activeId } : null
            );

            if (job.SkippedBy is not null)
            {
                job.Status = SyncJobStatus.Skipped;
                job.FinishedAt = job.CreatedAt;
                job.Progress = job.Progress.Stop();
            }

            db.SyncJobs.Add(SyncJobRecordMapper.ToRecord(job));
            SaveWithEviction(db);
            return job;
        }
    }

    public SyncJob? Get(JobId id)
    {
        using var db = dbFactory.CreateDbContext();
        var record = db.SyncJobs.AsNoTracking().FirstOrDefault(x => x.Id == id.Value);
        return record is null ? null : SyncJobRecordMapper.ToJob(record);
    }

    public IReadOnlyList<SyncJobSummary> GetAll(SyncJobStatus? status, SyncJobTrigger? trigger)
    {
        using var db = dbFactory.CreateDbContext();
        IQueryable<SyncJobRecord> query = db.SyncJobs;

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (trigger is not null)
        {
            query = query.Where(x => x.Trigger == trigger);
        }

        return SyncJobRecordMapper
            .ProjectToSummary(query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            .ToList();
    }

    public DateTimeOffset? LatestScheduledOccurrence()
    {
        using var db = dbFactory.CreateDbContext();
        return db
            .SyncJobs.Where(x => x.Trigger == SyncJobTrigger.Scheduled)
            .OrderByDescending(x => x.ScheduledFor)
            .Select(x => x.ScheduledFor)
            .FirstOrDefault();
    }

    public void Update(JobId id, Action<SyncJob> mutate)
    {
        lock (_gate)
        {
            using var db = dbFactory.CreateDbContext();
            var record = db.SyncJobs.FirstOrDefault(x => x.Id == id.Value);
            if (record is null || record.Status.IsTerminal())
            {
                return;
            }

            var job = SyncJobRecordMapper.ToJob(record);
            mutate(job);
            if (!record.Status.CanTransitionTo(job.Status))
            {
                throw new InvalidOperationException(
                    $"Sync job {id.Value} cannot move from {record.Status} to {job.Status}"
                );
            }

            SyncJobRecordMapper.Apply(job, record);
            SaveWithEviction(db);
        }
    }

    public void InterruptActive()
    {
        lock (_gate)
        {
            using var db = dbFactory.CreateDbContext();
            var records = db
                .SyncJobs.Where(x =>
                    x.Status == SyncJobStatus.Pending || x.Status == SyncJobStatus.Running
                )
                .ToList();

            var now = time.GetUtcNow();
            foreach (var record in records)
            {
                var job = SyncJobRecordMapper.ToJob(record);
                job.Progress = job.Progress.Stop();
                job.Status = SyncJobStatus.Interrupted;
                job.FinishedAt = now;
                SyncJobRecordMapper.Apply(job, record);
            }

            SaveWithEviction(db);
        }
    }

    private SyncJob NewJob(
        ServerSyncSettings request,
        IReadOnlyList<string> instanceNames,
        SyncJobTrigger trigger,
        DateTimeOffset? scheduledFor,
        JobId? skippedBy
    ) =>
        new()
        {
            Id = JobId.New(),
            Trigger = trigger,
            Request = request,
            CreatedAt = time.GetUtcNow(),
            ScheduledFor = scheduledFor,
            SkippedBy = skippedBy,
            Status = SyncJobStatus.Pending,
            Progress = new ProgressSnapshot(instanceNames),
        };

    public void AssignTicker(JobId id, Guid tickerId)
    {
        lock (_gate)
        {
            using var db = dbFactory.CreateDbContext();
            db.SyncJobs.Where(x => x.Id == id.Value)
                .ExecuteUpdate(x => x.SetProperty(r => r.TickerId, tickerId));
        }
    }

    // Caller must hold _gate. Writes the context's changes together with the eviction of terminal
    // jobs beyond the retention limit, then removes the evicted jobs' tickers. The tickers live in
    // TickerQ's context, so their removal is a separate write; a ticker left behind by a failure
    // there is inert because its job no longer exists.
    private void SaveWithEviction(ServerDbContext db)
    {
        var tickerIds = StageEviction(db);
        db.SaveChanges();

        if (tickerIds.Count == 0)
        {
            return;
        }

        using var queue = tickerQueueFactory.CreateDbContext();
        queue.Set<TimeTickerEntity>().Where(x => tickerIds.Contains(x.Id)).ExecuteDelete();
    }

    // Stages removal of terminal jobs beyond the retention limit, counting the jobs this context is
    // about to add or finish. Returns the tickers of the evicted jobs.
    private static List<Guid> StageEviction(ServerDbContext db)
    {
        var pending = db.ChangeTracker.Entries<SyncJobRecord>().Select(x => x.Entity).ToList();
        var pendingIds = pending.Select(x => x.Id).ToList();

        var stored = db
            .SyncJobs.Where(x =>
                !pendingIds.Contains(x.Id)
                && x.Status != SyncJobStatus.Pending
                && x.Status != SyncJobStatus.Running
            )
            .Select(x => new EvictionCandidate(x.Id, x.CreatedAt, x.TickerId))
            .ToList();

        var excess = stored
            .Concat(
                pending
                    .Where(x => x.Status.IsTerminal())
                    .Select(x => new EvictionCandidate(x.Id, x.CreatedAt, x.TickerId))
            )
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(MaxTerminalJobs)
            .ToList();

        foreach (var candidate in excess)
        {
            // Untracked jobs are removed through a key-only stub; the database cascades the
            // delete to their instance rows.
            db.SyncJobs.Remove(
                pending.Find(x => x.Id == candidate.Id) ?? new SyncJobRecord { Id = candidate.Id }
            );
        }

        return [.. excess.Select(x => x.TickerId).OfType<Guid>()];
    }

    private sealed record EvictionCandidate(Guid Id, DateTimeOffset CreatedAt, Guid? TickerId);
}
