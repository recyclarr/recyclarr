using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines;

internal class GenericSyncPipeline<TContext>(
    ILogger log,
    IProgressSource progressSource,
    IOrderedEnumerable<IPipelinePhase<TContext>> phases
) : ISyncPipeline
    where TContext : PipelineContext, IPipelineMetadata, new()
{
    public PipelineType PipelineType => TContext.PipelineType;
    public IReadOnlyList<PipelineType> Dependencies => TContext.Dependencies;

    public async Task<PipelineResult> Execute(
        ISyncSettings settings,
        PipelinePlan plan,
        CancellationToken ct
    )
    {
        var context = new TContext { SyncSettings = settings, Plan = plan };
        log.Debug("Executing Pipeline: {Pipeline}", context.PipelineDescription);

        if (context.ShouldSkip)
        {
            progressSource.SetPipelineStatus(PipelineProgressStatus.Skipped);
            return PipelineResult.Completed;
        }

        progressSource.SetPipelineStatus(PipelineProgressStatus.Running);

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
            progressSource.SetPipelineStatus(PipelineProgressStatus.Failed);
            return PipelineResult.Failed;
        }
        catch
        {
            progressSource.SetPipelineStatus(PipelineProgressStatus.Failed);
            throw;
        }
    }
}
