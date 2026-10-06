using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Persistence;

// Strict on purpose: an unmapped member on either side fails the build, so a field added to the
// job or its stored row cannot silently drop out of storage.
[Mapper]
internal static partial class SyncJobRecordMapper
{
    [MapPropertyFromSource(nameof(SyncJob.Request), Use = nameof(ToRequest))]
    [MapProperty(nameof(SyncJobRecord.SkippedByJobId), nameof(SyncJob.SkippedBy))]
    [MapperIgnoreSource(nameof(SyncJobRecord.Service))]
    [MapperIgnoreSource(nameof(SyncJobRecord.Instances))]
    [MapperIgnoreSource(nameof(SyncJobRecord.Preview))]
    [MapperIgnoreSource(nameof(SyncJobRecord.TickerId))]
    public static partial SyncJob ToJob(SyncJobRecord record);

    [MapProperty(nameof(SyncJob.SkippedBy), nameof(SyncJobRecord.SkippedByJobId))]
    [MapNestedProperties(nameof(SyncJob.Request))]
    [MapperIgnoreTarget(nameof(SyncJobRecord.TickerId))]
    public static partial SyncJobRecord ToRecord(SyncJob job);

    public static partial IQueryable<SyncJobSummary> ProjectToSummary(
        IQueryable<SyncJobRecord> query
    );

    // A summary is a deliberate subset of the row, so only its own members must be mapped.
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private static partial SyncJobSummary ToSummary(SyncJobRecord record);

    // Writes the mutable job state back; identity and the accepted request never change.
    public static void Apply(SyncJob job, SyncJobRecord record)
    {
        ApplyState(job, record);
        var rows = record.Progress.OrderBy(x => x.Ordinal);
        foreach (var (row, snapshot) in rows.Zip(job.Progress.Instances))
        {
            ApplyInstance(snapshot, row);
        }
    }

    // Update mappings check only the source: every job member must be written back or ignored,
    // while row-only members are covered by ToRecord. Unchanged values rewritten here (Trigger,
    // CreatedAt) are no-ops for change tracking.
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(SyncJob.Id))]
    [MapperIgnoreSource(nameof(SyncJob.Request))]
    [MapperIgnoreSource(nameof(SyncJob.SkippedBy))]
    [MapperIgnoreSource(nameof(SyncJob.Progress))]
    private static partial void ApplyState(SyncJob job, [MappingTarget] SyncJobRecord record);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapProperty(nameof(InstanceSnapshot.Result), nameof(SyncJobInstanceRecord.ResultJson))]
    [MapperIgnoreSource(nameof(InstanceSnapshot.Name))]
    private static partial void ApplyInstance(
        InstanceSnapshot snapshot,
        [MappingTarget] SyncJobInstanceRecord row
    );

    private static ServerSyncSettings ToRequest(SyncJobRecord record) =>
        new(record.Service, record.Instances, record.Preview);

    private static ProgressSnapshot ToProgress(List<SyncJobInstanceRecord> rows) =>
        ProgressSnapshot.Restore(rows.OrderBy(x => x.Ordinal).Select(ToSnapshot));

    [MapProperty(nameof(SyncJobInstanceRecord.ResultJson), nameof(InstanceSnapshot.Result))]
    [MapperIgnoreSource(nameof(SyncJobInstanceRecord.JobId))]
    [MapperIgnoreSource(nameof(SyncJobInstanceRecord.Job))]
    [MapperIgnoreSource(nameof(SyncJobInstanceRecord.Ordinal))]
    private static partial InstanceSnapshot ToSnapshot(SyncJobInstanceRecord row);

    // The foreign key is set by EF from the Progress navigation when the job is added.
    private static List<SyncJobInstanceRecord> ToRows(ProgressSnapshot progress) =>
        progress.Instances.Select((snapshot, ordinal) => ToRow(snapshot, ordinal)).ToList();

    [MapProperty(nameof(InstanceSnapshot.Result), nameof(SyncJobInstanceRecord.ResultJson))]
    [MapperIgnoreTarget(nameof(SyncJobInstanceRecord.JobId))]
    [MapperIgnoreTarget(nameof(SyncJobInstanceRecord.Job))]
    private static partial SyncJobInstanceRecord ToRow(InstanceSnapshot snapshot, int ordinal);

    private static JobId ToJobId(Guid id) => new() { Value = id };

    private static Guid ToGuid(JobId id) => id.Value;

    private static string ToResultJson(SyncInstanceResultsResponse result) =>
        StoredInstanceResult.Serialize(result);

    private static SyncInstanceResultsResponse ToResult(string json) =>
        StoredInstanceResult.Deserialize(json);
}
