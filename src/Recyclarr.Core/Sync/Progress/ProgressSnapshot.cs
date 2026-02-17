using System.Collections.Immutable;

namespace Recyclarr.Sync.Progress;

public readonly record struct ProgressSnapshot(ImmutableList<InstanceSnapshot> Instances);
