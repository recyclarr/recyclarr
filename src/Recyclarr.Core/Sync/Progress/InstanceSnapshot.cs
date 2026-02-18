using System.Collections.Immutable;

namespace Recyclarr.Sync.Progress;

public readonly record struct InstanceSnapshot(
    string Name,
    InstanceProgressStatus Status,
    ImmutableDictionary<PipelineType, PipelineSnapshot> Pipelines
)
{
    // Derives instance status from pipeline statuses (worst-status-wins).
    // Skipped is not checked independently because it only occurs when a dependency is Failed.
    public static InstanceProgressStatus DeriveStatus(
        ImmutableDictionary<PipelineType, PipelineSnapshot> pipelines
    )
    {
        if (pipelines.IsEmpty)
        {
            return InstanceProgressStatus.Pending;
        }

        var allTerminal = Enum.GetValues<PipelineType>()
            .All(type => pipelines.TryGetValue(type, out var p) && IsTerminal(p.Status));

        if (!allTerminal)
        {
            return InstanceProgressStatus.Running;
        }

        var statuses = pipelines.Values.Select(p => p.Status).ToList();

        if (statuses.Any(s => s == PipelineProgressStatus.Failed))
        {
            return InstanceProgressStatus.Failed;
        }

        if (statuses.Any(s => s == PipelineProgressStatus.Partial))
        {
            return InstanceProgressStatus.Partial;
        }

        if (statuses.Any(s => s == PipelineProgressStatus.Succeeded))
        {
            return InstanceProgressStatus.Succeeded;
        }

        // All pipelines are Skipped with none succeeded; instance failed
        return InstanceProgressStatus.Failed;
    }

    private static bool IsTerminal(PipelineProgressStatus status)
    {
        return status
            is PipelineProgressStatus.Succeeded
                or PipelineProgressStatus.Partial
                or PipelineProgressStatus.Failed
                or PipelineProgressStatus.Skipped;
    }
}
