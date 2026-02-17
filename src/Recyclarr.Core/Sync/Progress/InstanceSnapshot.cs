using System.Collections.Immutable;

namespace Recyclarr.Sync.Progress;

public readonly record struct InstanceSnapshot(
    string Name,
    InstanceProgressStatus Status,
    ImmutableDictionary<PipelineType, PipelineSnapshot> Pipelines
);
