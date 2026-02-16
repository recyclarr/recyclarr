using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Processors.Sync;

internal class CompositeSyncPipeline(
    ILogger log,
    IEnumerable<ISyncPipeline> pipelines,
    ISyncContextSource contextSource,
    IProgressSource progressSource,
    IServiceConfiguration config
) : IPipelineExecutor
{
    public virtual async Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        InstancePublisher instancePublisher,
        CancellationToken ct
    )
    {
        var sortedPipelines = TopologicalSort(pipelines);
        log.Debug(
            "Pipeline execution order: {Order}",
            string.Join(" -> ", sortedPipelines.Select(p => p.PipelineType))
        );

        var failedPipelines = new HashSet<PipelineType>();

        foreach (var pipeline in sortedPipelines)
        {
            contextSource.SetPipeline(pipeline.PipelineType);

            var progress = progressSource.ForPipeline(config.InstanceName, pipeline.PipelineType);
            var publisher = instancePublisher.ForPipeline(pipeline.PipelineType);

            var failedDependencies = pipeline.Dependencies.Where(failedPipelines.Contains).ToList();
            if (failedDependencies.Count > 0)
            {
                log.Debug(
                    "Skipping {Pipeline}: dependency {Dependency} failed",
                    pipeline.PipelineType,
                    failedDependencies[0]
                );
                progress.SetStatus(PipelineProgressStatus.Skipped);
                publisher.SetStatus(PipelineProgressStatus.Skipped);
                continue;
            }

            var result = await pipeline.Execute(settings, plan, progress, publisher, ct);
            log.Debug("Pipeline {Pipeline} result: {Result}", pipeline.PipelineType, result);

            if (result == PipelineResult.Failed)
            {
                failedPipelines.Add(pipeline.PipelineType);
            }
        }

        log.Information("Completed at {Date}", DateTime.Now);

        return failedPipelines.Count > 0 ? PipelineResult.Failed : PipelineResult.Completed;
    }

    private static List<ISyncPipeline> TopologicalSort(IEnumerable<ISyncPipeline> pipelines)
    {
        var pipelineList = pipelines.ToList();
        var pipelinesByType = pipelineList.ToDictionary(p => p.PipelineType);

        // Calculate in-degrees (number of dependencies each pipeline has that are in our set)
        var inDegree = pipelineList.ToDictionary(
            p => p.PipelineType,
            p => p.Dependencies.Count(d => pipelinesByType.ContainsKey(d))
        );

        // Start with pipelines that have no dependencies
        var queue = new Queue<ISyncPipeline>(
            pipelineList.Where(p => inDegree[p.PipelineType] == 0)
        );

        var result = new List<ISyncPipeline>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // Find pipelines that depend on the current one and decrement their in-degree
            foreach (
                var dependent in pipelineList.Where(p =>
                    p.Dependencies.Contains(current.PipelineType)
                )
            )
            {
                inDegree[dependent.PipelineType]--;
                if (inDegree[dependent.PipelineType] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return result.Count != pipelineList.Count
            ? throw new InvalidOperationException("Cycle detected in pipeline dependencies")
            : result;
    }
}
