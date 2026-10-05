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
            var record = NewRecord(request, instanceNames, SyncJobTrigger.Manual);
            db.SyncJobs.Add(record);
            db.SaveChanges();
            return ToJob(record);
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
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();

            var record = NewRecord(request, instanceNames, SyncJobTrigger.Scheduled);
            record.ScheduledFor = scheduledFor;

            if (active is not null)
            {
                record.Status = SyncJobStatus.Skipped;
                record.SkippedByJobId = active;
                record.FinishedAt = record.CreatedAt;
                foreach (var instance in record.Progress)
                {
                    instance.Status = InstanceProgressStatus.NotRun;
                }
            }

            db.SyncJobs.Add(record);
            db.SaveChanges();

            if (record.Status.IsTerminal())
            {
                EvictExcessTerminalJobs(db);
            }

            return ToJob(record);
        }
    }

    public SyncJob? Get(JobId id)
    {
        using var db = dbFactory.CreateDbContext();
        var record = db.SyncJobs.AsNoTracking().FirstOrDefault(x => x.Id == id.Value);
        return record is null ? null : ToJob(record);
    }

    public IReadOnlyList<SyncJob> GetAll(SyncJobStatus? status, SyncJobTrigger? trigger)
    {
        using var db = dbFactory.CreateDbContext();
        var query = db.SyncJobs.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (trigger is not null)
        {
            query = query.Where(x => x.Trigger == trigger);
        }

        return query.OrderBy(x => x.CreatedAt).AsEnumerable().Select(ToJob).ToList();
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

            var job = ToJob(record);
            mutate(job);
            if (!record.Status.CanTransitionTo(job.Status))
            {
                throw new InvalidOperationException(
                    $"Sync job {id.Value} cannot move from {record.Status} to {job.Status}"
                );
            }

            Apply(job, record);
            db.SaveChanges();

            if (job.Status.IsTerminal())
            {
                EvictExcessTerminalJobs(db);
            }
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
                var job = ToJob(record);
                job.Progress = job.Progress.Stop();
                job.Status = SyncJobStatus.Interrupted;
                job.FinishedAt = now;
                Apply(job, record);
            }

            db.SaveChanges();
            EvictExcessTerminalJobs(db);
        }
    }

    private SyncJobRecord NewRecord(
        ServerSyncSettings request,
        IReadOnlyList<string> instanceNames,
        SyncJobTrigger trigger
    )
    {
        var id = JobId.New().Value;
        return new SyncJobRecord
        {
            Id = id,
            Trigger = trigger,
            Status = SyncJobStatus.Pending,
            CreatedAt = time.GetUtcNow(),
            Service = request.Service,
            Instances = request.Instances.ToList(),
            Preview = request.Preview,
            Progress = instanceNames
                .Select(
                    (name, i) =>
                        new SyncJobInstanceRecord
                        {
                            JobId = id,
                            Ordinal = i,
                            Name = name,
                            Status = InstanceProgressStatus.Pending,
                        }
                )
                .ToList(),
        };
    }

    // Caller must hold _gate.
    private static void EvictExcessTerminalJobs(ServerDbContext db)
    {
        var terminal = db
            .SyncJobs.Where(x =>
                x.Status != SyncJobStatus.Pending && x.Status != SyncJobStatus.Running
            )
            .OrderByDescending(x => x.CreatedAt)
            .Skip(MaxTerminalJobs)
            .ToList();

        if (terminal.Count == 0)
        {
            return;
        }

        db.SyncJobs.RemoveRange(terminal);
        db.SaveChanges();
    }

    private static SyncJob ToJob(SyncJobRecord record) =>
        new()
        {
            Id = new JobId { Value = record.Id },
            Trigger = record.Trigger,
            Request = new ServerSyncSettings(record.Service, record.Instances, record.Preview),
            CreatedAt = record.CreatedAt,
            ScheduledFor = record.ScheduledFor,
            SkippedBy = record.SkippedByJobId is { } skippedBy
                ? new JobId { Value = skippedBy }
                : null,
            Status = record.Status,
            StartedAt = record.StartedAt,
            FinishedAt = record.FinishedAt,
            FaultReference = record.FaultReference,
            Progress = ProgressSnapshot.Restore(
                record
                    .Progress.OrderBy(x => x.Ordinal)
                    .Select(x => new InstanceSnapshot(
                        x.Name,
                        x.Status,
                        x.ResultJson is null ? null : StoredInstanceResult.Deserialize(x.ResultJson)
                    ))
            ),
        };

    // Writes the mutable job state back; identity and the accepted request never change.
    private static void Apply(SyncJob job, SyncJobRecord record)
    {
        record.Status = job.Status;
        record.StartedAt = job.StartedAt;
        record.FinishedAt = job.FinishedAt;
        record.FaultReference = job.FaultReference;

        var instances = record.Progress.OrderBy(x => x.Ordinal).ToList();
        foreach (var (row, snapshot) in instances.Zip(job.Progress.Instances))
        {
            row.Status = snapshot.Status;
            row.ResultJson = snapshot.Result is null
                ? null
                : StoredInstanceResult.Serialize(snapshot.Result);
        }
    }
}
