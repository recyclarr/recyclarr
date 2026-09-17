using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Progress;

internal sealed record InstanceSnapshot(
    string Name,
    InstanceProgressStatus Status,
    SyncInstanceResult? Result
);
