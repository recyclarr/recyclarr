using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncPipelineExecutor
{
    private readonly ILogger _log;
    private readonly IOrderedEnumerable<ISyncPipeline> _pipelines;
    private readonly IEnumerable<IPipelineCache> _caches;

    public SyncPipelineExecutor(
        ILogger log,
        IOrderedEnumerable<ISyncPipeline> pipelines,
        IEnumerable<IPipelineCache> caches)
    {
        _log = log;
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
            _log.Debug("Executing Pipeline: {Pipeline}", pipeline.GetType().Name);
            await pipeline.Execute(settings, config);
        }
    }
}
