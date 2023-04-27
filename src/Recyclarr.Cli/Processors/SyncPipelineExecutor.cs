using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Processors;

public class SyncPipelineExecutor
{
    private readonly IOrderedEnumerable<ISyncPipeline> _pipelines;
    private readonly IEnumerable<IPipelineCache> _caches;

    public SyncPipelineExecutor(IOrderedEnumerable<ISyncPipeline> pipelines, IEnumerable<IPipelineCache> caches)
    {
        _pipelines = pipelines;
        _caches = caches;
    }

    public async Task Process(ISyncSettings settings, IServiceConfiguration config)
    {
        foreach (var cache in _caches)
        {
            cache.Clear();
        }

        foreach (var pipeline in _pipelines)
        {
            await pipeline.Execute(settings, config);
        }
    }
}
