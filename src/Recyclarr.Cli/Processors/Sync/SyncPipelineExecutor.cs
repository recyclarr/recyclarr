using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncPipelineExecutor(
    ILogger log,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches)
{
    public async Task Process(ISyncSettings settings, IServiceConfiguration config)
    {
        foreach (var cache in caches)
        {
            cache.Clear();
        }

        foreach (var pipeline in pipelines)
        {
            log.Debug("Executing Pipeline: {Pipeline}", pipeline.GetType().Name);
            await pipeline.Execute(settings, config);
        }
    }
}
