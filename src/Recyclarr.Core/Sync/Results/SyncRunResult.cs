namespace Recyclarr.Sync.Results;

/// <summary>
/// The ordered terminal instance results and optional unexpected fault for one sync run.
/// </summary>
public sealed record SyncRunResult
{
    public SyncRunResult(IReadOnlyList<SyncInstanceResult> instances, SyncFault? fault = null)
    {
        ArgumentNullException.ThrowIfNull(instances);

        Instances = instances.ToList().AsReadOnly();
        Fault = fault;
        Status = SyncResultStatusAggregation.From(
            Instances.Select(x => x.Status),
            fault is not null
        );
    }

    public SyncResultStatus Status { get; }
    public IReadOnlyList<SyncInstanceResult> Instances { get; }
    public SyncFault? Fault { get; }
}
