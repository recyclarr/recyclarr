using System.Collections.Immutable;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Progress;

internal sealed record ProgressSnapshot
{
    public ProgressSnapshot(IEnumerable<string> instanceNames)
        : this(
            instanceNames
                .Select(name => new InstanceSnapshot(name, InstanceProgressStatus.Pending, null))
                .ToImmutableList()
        ) { }

    private ProgressSnapshot(ImmutableList<InstanceSnapshot> instances)
    {
        Instances = instances;
    }

    // Rebuilds a snapshot read back from storage, in selection order.
    public static ProgressSnapshot Restore(IEnumerable<InstanceSnapshot> instances) =>
        new(instances.ToImmutableList());

    public ImmutableList<InstanceSnapshot> Instances { get; }

    // At least one instance finished with changes applied, so a run that stops early is partial
    // rather than failed.
    public bool HasCompletedWork =>
        Instances.Any(instance =>
            instance.Status is InstanceProgressStatus.Succeeded or InstanceProgressStatus.Partial
        );

    public ProgressSnapshot Start(string instanceName) =>
        Update(
            instanceName,
            instance =>
                instance.Status == InstanceProgressStatus.Pending
                    ? instance with
                    {
                        Status = InstanceProgressStatus.Running,
                    }
                    : null
        );

    public ProgressSnapshot Complete(SyncInstanceResultsResponse result) =>
        Update(
            result.Name,
            instance =>
                instance.Status == InstanceProgressStatus.Running
                    ? instance with
                    {
                        Status = ToProgressStatus(result.Status),
                        Result = result,
                    }
                    : null
        );

    public ProgressSnapshot Stop()
    {
        var changed = false;
        var instances = Instances
            .Select(instance =>
            {
                var status = instance.Status switch
                {
                    InstanceProgressStatus.Pending => InstanceProgressStatus.NotRun,
                    InstanceProgressStatus.Running => InstanceProgressStatus.Interrupted,
                    _ => instance.Status,
                };
                changed |= status != instance.Status;
                return instance with { Status = status };
            })
            .ToImmutableList();

        return changed ? new ProgressSnapshot(instances) : this;
    }

    public ProgressSnapshot Reconcile(IEnumerable<SyncInstanceResultsResponse> results)
    {
        var snapshot = this;
        foreach (var instanceResult in results)
        {
            snapshot = snapshot.Reconcile(instanceResult);
        }

        return snapshot;
    }

    private ProgressSnapshot Update(
        string instanceName,
        Func<InstanceSnapshot, InstanceSnapshot?> update
    )
    {
        var index = Instances.FindIndex(instance =>
            instance.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)
        );
        if (index < 0 || update(Instances[index]) is not { } updated)
        {
            return this;
        }

        return new ProgressSnapshot(Instances.SetItem(index, updated));
    }

    private ProgressSnapshot Reconcile(SyncInstanceResultsResponse result) =>
        Update(
            result.Name,
            instance =>
                instance.Result is null
                    ? instance with
                    {
                        Status = ToProgressStatus(result.Status),
                        Result = result,
                    }
                    : null
        );

    private static InstanceProgressStatus ToProgressStatus(SyncCompletionStatus status) =>
        status switch
        {
            SyncCompletionStatus.Succeeded => InstanceProgressStatus.Succeeded,
            SyncCompletionStatus.Partial => InstanceProgressStatus.Partial,
            SyncCompletionStatus.Failed => InstanceProgressStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
