using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines;

internal class GenericSyncPipeline<TContext>(
    ILogger log,
    IOrderedEnumerable<IPipelinePhase<TContext>> phases,
    IServiceConfiguration config
) : ISyncPipeline
    where TContext : PipelineContext, IPipelineMetadata, new()
{
    public PipelineType PipelineType => TContext.PipelineType;
    public IReadOnlyList<PipelineType> Dependencies => TContext.Dependencies;

    public async Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var context = new TContext
        {
            InstanceName = config.InstanceName,
            SyncSettings = settings,
            Publisher = publisher,
            Plan = plan,
        };
        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);

        if (context.ShouldSkip)
        {
            publisher.SetStatus(PipelineProgressStatus.Skipped);
            return PipelineResult.Completed;
        }

        publisher.SetStatus(PipelineProgressStatus.Running);

        try
        {
            foreach (var phase in phases)
            {
                var flow = await phase.Execute(context, ct);
                if (flow == PipelineFlow.Terminate)
                {
                    break;
                }
            }

            return PipelineResult.Completed;
        }
        catch (PipelineInterruptException)
        {
            publisher.SetStatus(PipelineProgressStatus.Failed);
            return PipelineResult.Failed;
        }
        catch
        {
            publisher.SetStatus(PipelineProgressStatus.Failed);
            throw;
        }
    }
}
