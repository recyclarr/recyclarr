using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncPipelineExecutor(
    ILogger log,
    IAnsiConsole console,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches,
    ServiceAgnosticCapabilityEnforcer enforcer,
    IServiceConfiguration config,
    NotificationEmitter emitter)
{
    public async Task Process(ISyncSettings settings)
    {
        PrintProcessingHeader();
        // TODO this is temporary for testing ONLY!!!!!!!
        await enforcer.Check(config);
        emitter.NotifyStatistic("Stat 1", 100);
        emitter.NotifyStatistic("Stat 2", 200);
        foreach (var cache in caches)
        {
            cache.Clear();
        }

        foreach (var pipeline in pipelines)
        {
            log.Debug("Executing Pipeline: {Pipeline}", pipeline.GetType().Name);
            await pipeline.Execute(settings);
        }

        log.Information("Completed at {Date}", DateTime.Now);
    }

    private void PrintProcessingHeader()
    {
        var instanceName = config.InstanceName;

        console.WriteLine(
            $"""

             ===========================================
             Processing {config.ServiceType} Server: [{instanceName}]
             ===========================================

             """);

        log.Debug("Processing {Server} server {Name}", config.ServiceType, instanceName);
    }
}
