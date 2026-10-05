using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Progress;

// Result is the instance's v1 results representation: what the job stores and serves after the
// run's domain objects are gone.
internal sealed record InstanceSnapshot(
    string Name,
    InstanceProgressStatus Status,
    SyncInstanceResultsResponse? Result
);
