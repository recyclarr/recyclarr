using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Pipelines;

internal class GenericSyncPipeline<TContext>(
    ILogger log,
    IOrderedEnumerable<IPipelinePhase<TContext>> phases
) : ISyncPipeline
    where TContext : PipelineContext, new()
{
    public async Task Execute(ISyncSettings settings, CancellationToken ct)
    {
        var context = new TContext { SyncSettings = settings };

        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);

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
