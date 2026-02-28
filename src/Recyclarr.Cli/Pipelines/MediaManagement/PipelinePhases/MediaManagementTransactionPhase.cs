namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementTransactionPhase : IPipelinePhase<MediaManagementPipelineContext>
{
    public Task<PipelineFlow> Execute(MediaManagementPipelineContext context, CancellationToken ct)
    {
        // non-null: ShouldSkip guarantees MediaManagement is set before this phase runs
        var planned = context.Plan.MediaManagement!;

        context.TransactionOutput = context.ApiFetchOutput with
        {
            PropersAndRepacks = planned.PropersAndRepacks,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
