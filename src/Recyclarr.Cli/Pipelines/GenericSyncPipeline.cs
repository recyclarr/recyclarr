using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Pipelines;

internal class GenericSyncPipeline<TContext>(
    ILogger log,
    IReadOnlyCollection<IPipelinePhase<TContext>> phases
) : ISyncPipeline
    where TContext : PipelineContext, new()
{
    public async Task Execute(ISyncSettings settings, CancellationToken ct)
    {
        var context = new TContext { SyncSettings = settings };

        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);

        foreach (var phase in phases)
        {
            if (!await phase.Execute(context, ct))
            {
                break;
            }
        }
    }
}
