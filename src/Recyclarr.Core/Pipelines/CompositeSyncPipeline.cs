using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines;

internal class CompositeSyncPipeline(ILogger log, IEnumerable<ISyncOperation> operations)
    : IPipelineExecutor
{
    public virtual async Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IInstancePublisher instancePublisher,
        CancellationToken ct
    )
    {
        // Filter before TopologicalSort: plan components already encode service affinity
        // (e.g. SonarrMediaNamingAvailable is false for Radarr instances), so ShouldSkip
        // resolves duplicate PipelineType keys (both naming ops share MediaNaming).
        var (applicable, skipped) = PartitionBySkip(operations, plan);

        foreach (var operation in skipped)
        {
            instancePublisher.ForPipeline(operation.Type).SetStatus(PipelineProgressStatus.Skipped);
        }

        var sortedOperations = TopologicalSort(applicable);
        log.Debug(
            "Sync operation order: {Order}",
            string.Join(" -> ", sortedOperations.Select(o => o.Type))
        );

        var failedOperations = new HashSet<PipelineType>();

        foreach (var operation in sortedOperations)
        {
            var publisher = instancePublisher.ForPipeline(operation.Type);

            // Plan errors mean nothing can run; mark all operations skipped so the progress
            // table shows `--` across the row and DeriveStatus infers instance Failed.
            if (plan.HasErrors)
            {
                publisher.SetStatus(PipelineProgressStatus.Skipped);
                continue;
            }

            var failedDependencies = operation
                .Dependencies.Where(failedOperations.Contains)
                .ToList();
            if (failedDependencies.Count > 0)
            {
                log.Debug(
                    "Skipping {Operation}: dependency {Dependency} failed",
                    operation.Type,
                    failedDependencies[0]
                );
                publisher.SetStatus(PipelineProgressStatus.Skipped);
                continue;
            }

            publisher.SetStatus(PipelineProgressStatus.Running);

            try
            {
                var result = await operation.Compute(plan, publisher, ct);

                if (!settings.Preview)
                {
                    await operation.Persist(result, publisher, ct);
                }
            }
            catch (PipelineInterruptException)
            {
                publisher.SetStatus(PipelineProgressStatus.Failed);
                failedOperations.Add(operation.Type);
            }
            catch
            {
                publisher.SetStatus(PipelineProgressStatus.Failed);
                throw;
            }
        }

        log.Information("Completed at {Date}", DateTime.Now);

        return failedOperations.Count > 0 || plan.HasErrors
            ? PipelineResult.Failed
            : PipelineResult.Completed;
    }

    public void InterruptAll(IInstancePublisher instancePublisher)
    {
        foreach (var operation in operations)
        {
            instancePublisher
                .ForPipeline(operation.Type)
                .SetStatus(PipelineProgressStatus.Interrupted);
        }
    }

    private static (List<ISyncOperation> Applicable, List<ISyncOperation> Skipped) PartitionBySkip(
        IEnumerable<ISyncOperation> operations,
        PipelinePlan plan
    )
    {
        var applicable = new List<ISyncOperation>();
        var skipped = new List<ISyncOperation>();

        foreach (var op in operations)
        {
            (op.ShouldSkip(plan) ? skipped : applicable).Add(op);
        }

        return (applicable, skipped);
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
