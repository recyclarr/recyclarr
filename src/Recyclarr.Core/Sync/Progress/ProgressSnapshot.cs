using System.Collections.Immutable;

namespace Recyclarr.Sync.Progress;

public enum InstanceProgressStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
}

public readonly record struct PipelineSnapshot(PipelineProgressStatus Status, int? Count);

public readonly record struct InstanceSnapshot(
    string Name,
    InstanceProgressStatus Status,
    ImmutableDictionary<PipelineType, PipelineSnapshot> Pipelines
);

public readonly record struct ProgressSnapshot(ImmutableList<InstanceSnapshot> Instances);
