using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Processors.Sync;

public class CompositeSyncPipeline(
    ILogger log,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches,
    ServiceAgnosticCapabilityEnforcer enforcer,
    IServiceConfiguration config) : ISyncPipeline
{
    public virtual async Task Execute(ISyncSettings settings, CancellationToken ct)
    {
        log.Debug("Processing {Server} server {Name}", config.ServiceType, config.InstanceName);

        await enforcer.Check(config, ct);

        foreach (var cache in caches)
        {
            cache.Clear();
        }

        foreach (var pipeline in pipelines)
        {
            await pipeline.Execute(settings, ct);
        }

        log.Information("Completed at {Date}", DateTime.Now);
    }
}
