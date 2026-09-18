using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines;

internal class CompositeSyncPipeline(ILogger log, IEnumerable<ISyncOperation> operations)
    : IPipelineExecutor
{
    public virtual async Task<IReadOnlyList<PipelineResult>> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        PipelineExecutionBuffer buffer,
        CancellationToken ct
    )
    {
        // Filter before TopologicalSort: plan components already encode service affinity
        // (e.g. SonarrMediaNamingAvailable is false for Radarr instances), so ShouldSkip
        // resolves duplicate PipelineType keys (both naming ops share MediaNaming).
        var applicable = operations.Where(operation => !operation.ShouldSkip(plan)).ToList();
        var sortedOperations = TopologicalSort(applicable);
        log.Debug(
            "Sync operation order: {Order}",
            string.Join(" -> ", sortedOperations.Select(o => o.Type))
        );

        var completedOperations = new Dictionary<PipelineType, PipelineResult>();

        foreach (var operation in sortedOperations)
        {
            var failedDependencies = operation
                .Dependencies.Where(dependency =>
                    completedOperations.TryGetValue(dependency, out var result)
                    && !result.Status.SatisfiesDependency()
                )
                .ToList();
            if (failedDependencies.Count > 0)
            {
                log.Debug(
                    "Skipping {Operation}: dependency {Dependency} failed",
                    operation.Type,
                    failedDependencies[0]
                );
                var blocked = operation.CreateBlockedResult(failedDependencies[0]);
                completedOperations.Add(operation.Type, blocked);
                buffer.Capture(operation.Type, blocked);
                continue;
            }

            if (plan.HasInstanceBlockingErrors)
            {
                var failed = operation.CreateFailedResult(null);
                completedOperations.Add(operation.Type, failed);
                buffer.Capture(operation.Type, failed);
                continue;
            }

            try
            {
                var result = await operation.Execute(
                    settings.Preview,
                    plan,
                    result => buffer.Capture(operation.Type, result),
                    ct
                );
                completedOperations.Add(operation.Type, result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                var current = buffer.Get(operation.Type);
                var failed = operation.CreateFailedResult(current);
                buffer.Capture(operation.Type, failed);
                throw;
            }
        }

        log.Information("Completed at {Date}", DateTime.Now);

        return buffer.Results;
    }

    private static List<ISyncOperation> TopologicalSort(IEnumerable<ISyncOperation> operations)
    {
        var operationList = operations.ToList();
        var operationsByType = operationList.ToDictionary(o => o.Type);

        // Calculate in-degrees (number of dependencies each operation has that are in our set)
        var inDegree = operationList.ToDictionary(
            o => o.Type,
            o => o.Dependencies.Count(d => operationsByType.ContainsKey(d))
        );

        // Start with operations that have no dependencies
        var queue = new Queue<ISyncOperation>(operationList.Where(o => inDegree[o.Type] == 0));

        var result = new List<ISyncOperation>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // Find operations that depend on the current one and decrement their in-degree
            foreach (
                var dependent in operationList.Where(o => o.Dependencies.Contains(current.Type))
            )
            {
                inDegree[dependent.Type]--;
                if (inDegree[dependent.Type] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return result.Count != operationList.Count
            ? throw new InvalidOperationException("Cycle detected in pipeline dependencies")
            : result;
    }
}
