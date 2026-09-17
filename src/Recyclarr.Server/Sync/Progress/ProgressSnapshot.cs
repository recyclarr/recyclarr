using System.Collections.Immutable;
using Recyclarr.Sync.Results;

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

    public ImmutableList<InstanceSnapshot> Instances { get; }

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

    public ProgressSnapshot Complete(SyncInstanceResult result) =>
        Update(
            result.InstanceName,
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

    public ProgressSnapshot Reconcile(SyncRunResult result)
    {
        var snapshot = this;
        foreach (var instanceResult in result.Instances)
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

    private ProgressSnapshot Reconcile(SyncInstanceResult result) =>
        Update(
            result.InstanceName,
            instance =>
                instance.Result is null
                    ? instance with
                    {
                        Status = ToProgressStatus(result.Status),
                        Result = result,
                    }
                    : null
        );

    private static InstanceProgressStatus ToProgressStatus(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => InstanceProgressStatus.Succeeded,
            SyncResultStatus.Partial => InstanceProgressStatus.Partial,
            SyncResultStatus.Failed or SyncResultStatus.Blocked => InstanceProgressStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
