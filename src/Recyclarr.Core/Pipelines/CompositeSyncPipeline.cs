using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines;

internal class CompositeSyncPipeline(ILogger log, PipelineGraph graph) : IPipelineExecutor
{
    public virtual async Task<IReadOnlyList<PipelineResult>> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        PipelineExecutionBuffer buffer,
        CancellationToken ct
    )
    {
        log.Debug(
            "Sync operation order: {Order}",
            string.Join(" -> ", graph.Pipelines.Select(o => o.Type))
        );

        var completedOperations = new Dictionary<PipelineType, PipelineResult>();

        foreach (var pipeline in graph.Pipelines)
        {
            // A pipeline without a plan has nothing configured, so it reports no result
            if (!pipeline.HasPlan(plan))
            {
                continue;
            }

            var failedDependencies = pipeline
                .Dependencies.Where(dependency =>
                    completedOperations.TryGetValue(dependency, out var result)
                    && !result.Status.SatisfiesDependency()
                )
                .ToList();
            if (failedDependencies.Count > 0)
            {
                log.Debug(
                    "Skipping {Operation}: dependency {Dependency} failed",
                    pipeline.Type,
                    failedDependencies[0]
                );
                var blocked = pipeline.CreateBlockedResult(failedDependencies[0]);
                completedOperations.Add(pipeline.Type, blocked);
                buffer.Capture(pipeline.Type, blocked);
                continue;
            }

            if (plan.HasInstanceBlockingErrors)
            {
                var failed = pipeline.CreateFailedResult(null);
                completedOperations.Add(pipeline.Type, failed);
                buffer.Capture(pipeline.Type, failed);
                continue;
            }

            try
            {
                var result = await pipeline.Execute(
                    settings.Preview,
                    plan,
                    result => buffer.Capture(pipeline.Type, result),
                    ct
                );
                completedOperations.Add(pipeline.Type, result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                var current = buffer.Get(pipeline.Type);
                var failed = pipeline.CreateFailedResult(current);
                buffer.Capture(pipeline.Type, failed);
                throw;
            }
        }

        log.Information("Completed at {Date}", DateTime.Now);

        return buffer.Results;
    }
}
