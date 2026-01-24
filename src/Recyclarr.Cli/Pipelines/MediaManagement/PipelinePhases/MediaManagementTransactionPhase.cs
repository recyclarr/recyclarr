namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementTransactionPhase : IPipelinePhase<MediaManagementPipelineContext>
{
    public Task<PipelineFlow> Execute(MediaManagementPipelineContext context, CancellationToken ct)
    {
        var planned = context.Plan.MediaManagement;

        context.TransactionOutput = context.ApiFetchOutput with
        {
            DownloadPropersAndRepacks = planned.PropersAndRepacks,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
