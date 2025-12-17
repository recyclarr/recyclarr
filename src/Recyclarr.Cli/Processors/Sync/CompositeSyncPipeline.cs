using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Processors.Sync;

internal class CompositeSyncPipeline(
    ILogger log,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches,
    ServiceAgnosticCapabilityEnforcer enforcer,
    IServiceConfiguration config
) : ISyncPipeline
{
    public virtual async Task Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        CancellationToken ct
    )
    {
        log.Information(
            "Processing {Server} server {Name}",
            config.ServiceType,
            config.InstanceName
        );

        await enforcer.Check(config, ct);

        foreach (var cache in caches)
        {
            cache.Clear();
        }

        foreach (var pipeline in pipelines)
        {
            await pipeline.Execute(settings, plan, ct);
        }

        log.Information("Completed at {Date}", DateTime.Now);
    }
}
