using Microsoft.EntityFrameworkCore;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;

namespace Recyclarr.Server.Persistence;

internal sealed class DatabaseSyncJobStore(
    IDbContextFactory<ServerDbContext> dbFactory,
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
            StageEviction(db);
            db.SaveChanges();
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
            StageEviction(db);
            db.SaveChanges();
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

            StageEviction(db);
            db.SaveChanges();
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

    // Caller must hold _gate. Stages removal of terminal jobs beyond the retention limit, counting
    // the jobs this context is about to add or finish, so one SaveChanges writes both.
    private static void StageEviction(ServerDbContext db)
    {
        var pending = db.ChangeTracker.Entries<SyncJobRecord>().Select(x => x.Entity).ToList();
        var pendingIds = pending.Select(x => x.Id).ToList();

        var stored = db
            .SyncJobs.Where(x =>
                !pendingIds.Contains(x.Id)
                && x.Status != SyncJobStatus.Pending
                && x.Status != SyncJobStatus.Running
            )
            .Select(x => new { x.Id, x.CreatedAt })
            .ToList();

        var excess = stored
            .Concat(
                pending.Where(x => x.Status.IsTerminal()).Select(x => new { x.Id, x.CreatedAt })
            )
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(MaxTerminalJobs)
            .Select(x => x.Id);

        foreach (var id in excess)
        {
            // Untracked jobs are removed through a key-only stub; the database cascades the
            // delete to their instance rows.
            db.SyncJobs.Remove(pending.Find(x => x.Id == id) ?? new SyncJobRecord { Id = id });
        }
    }
}
