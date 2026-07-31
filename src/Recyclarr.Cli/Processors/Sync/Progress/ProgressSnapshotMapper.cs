using System.Collections.Immutable;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Processors.Sync.Progress;

/// <summary>
/// Turns the wire representation of sync progress back into the snapshot types the progress table
/// renders from. Statuses and pipeline types travel as their names; anything unrecognized is
/// dropped rather than crashing a display, which keeps an older CLI usable against a newer server.
/// </summary>
internal static class ProgressSnapshotMapper
{
    public static ProgressSnapshot ToSnapshot(IEnumerable<InstanceSnapshotResponse> instances)
    {
        return new ProgressSnapshot([.. instances.Select(ToInstanceSnapshot)]);
    }

    private static InstanceSnapshot ToInstanceSnapshot(InstanceSnapshotResponse instance)
    {
        var pipelines = instance
            .Pipelines.Select(p => (Parsed: Parse(p), Snapshot: p))
            .Where(x => x.Parsed is not null)
            .ToImmutableDictionary(
                // non-null: filtered above
                x => x.Parsed!.Value.Type,
                x => new PipelineSnapshot(
                    x.Parsed!.Value.Status,
                    x.Snapshot.Count,
                    ToChanges(x.Snapshot.Changes)
                )
            );

        return new InstanceSnapshot(
            instance.Name,
            Enum.TryParse<InstanceProgressStatus>(instance.Status, out var status)
                ? status
                : InstanceProgressStatus.Pending,
            pipelines
        );
    }

    private static (PipelineType Type, PipelineProgressStatus Status)? Parse(
        PipelineSnapshotResponse pipeline
    )
    {
        return
            Enum.TryParse<PipelineType>(pipeline.Type, out var type)
            && Enum.TryParse<PipelineProgressStatus>(pipeline.Status, out var status)
            ? (type, status)
            : null;
    }

    private static PipelineItemChanges? ToChanges(PipelineItemChangesResponse? changes)
    {
        return changes is null
            ? null
            : new PipelineItemChanges(
                [.. changes.Created],
                [.. changes.Updated],
                [.. changes.Deleted]
            );
    }
}
