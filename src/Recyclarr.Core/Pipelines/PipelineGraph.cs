using Recyclarr.Sync;

namespace Recyclarr.Pipelines;

/// <summary>
/// The registered pipelines in dependency order. Construction rejects duplicate pipelines or plan
/// types, dependencies on unregistered pipelines, and cycles, so a bad registration fails before
/// any planning starts. For a given registration order, the resulting order is deterministic.
/// </summary>
internal sealed class PipelineGraph
{
    private readonly Dictionary<PipelineType, IPipeline> _byType;

    public PipelineGraph(IEnumerable<IPipeline> pipelines)
    {
        var registered = pipelines.ToList();
        ThrowOnDuplicates(registered.Select(x => x.Type), "pipeline");
        ThrowOnDuplicates(registered.Select(x => x.PlanType), "plan type");

        _byType = registered.ToDictionary(x => x.Type);
        foreach (var pipeline in registered)
        {
            var missing = pipeline.Dependencies.Where(x => !_byType.ContainsKey(x)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Pipeline {pipeline.Type} depends on unregistered pipelines: "
                        + string.Join(", ", missing)
                );
            }
        }

        Pipelines = Sort(registered);
    }

    public IReadOnlyList<IPipeline> Pipelines { get; }

    public IPipeline Get(PipelineType type) => _byType[type];

    private static void ThrowOnDuplicates<T>(IEnumerable<T> keys, string kind)
    {
        var duplicates = keys.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        var list = duplicates.ToList();
        if (list.Count > 0)
        {
            throw new InvalidOperationException(
                $"Each {kind} must be registered once: {string.Join(", ", list)}"
            );
        }
    }

    // Kahn's algorithm: ready pipelines are queued in registration order
    private static List<IPipeline> Sort(List<IPipeline> pipelines)
    {
        var inDegree = pipelines.ToDictionary(x => x.Type, x => x.Dependencies.Count);
        var queue = new Queue<IPipeline>(pipelines.Where(x => inDegree[x.Type] == 0));
        var result = new List<IPipeline>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var dependent in pipelines.Where(x => x.Dependencies.Contains(current.Type)))
            {
                if (--inDegree[dependent.Type] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (result.Count != pipelines.Count)
        {
            var cyclic = pipelines.Except(result).Select(x => x.Type);
            throw new InvalidOperationException(
                $"Cycle detected in pipeline dependencies: {string.Join(", ", cyclic)}"
            );
        }

        return result;
    }
}
