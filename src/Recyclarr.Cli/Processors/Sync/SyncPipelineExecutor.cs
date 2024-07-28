using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

public class SyncPipelineExecutor(
    ILogger log,
    IAnsiConsole console,
    IOrderedEnumerable<ISyncPipeline> pipelines,
    IEnumerable<IPipelineCache> caches,
    ServiceAgnosticCapabilityEnforcer enforcer,
    IServiceConfiguration config)
{
    public async Task Process(ISyncSettings settings, CancellationToken ct)
    {
        PrintProcessingHeader();

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
