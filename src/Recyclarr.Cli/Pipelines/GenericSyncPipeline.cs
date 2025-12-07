using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines;

internal class GenericSyncPipeline<TContext>(
    ILogger log,
    ISyncEventCollector eventCollector,
    IOrderedEnumerable<IPipelinePhase<TContext>> phases
) : ISyncPipeline
    where TContext : PipelineContext, new()
{
    public async Task Execute(ISyncSettings settings, PipelinePlan plan, CancellationToken ct)
    {
        var context = new TContext { SyncSettings = settings, Plan = plan };

        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);
        eventCollector.SetPipeline(context.PipelineType);

        foreach (var phase in phases)
        {
            var flow = await phase.Execute(context, ct);
            if (flow == PipelineFlow.Terminate)
            {
                break;
            }
        }
    }
}
