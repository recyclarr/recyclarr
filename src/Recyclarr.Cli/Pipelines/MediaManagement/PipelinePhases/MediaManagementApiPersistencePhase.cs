using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementApiPersistencePhase(ILogger log, IMediaManagementService api)
    : IPipelinePhase<MediaManagementPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        MediaManagementPipelineContext context,
        CancellationToken ct
    )
    {
        var differences = context.ApiFetchOutput.GetDifferences(context.TransactionOutput);

        if (differences.Count != 0)
        {
            await api.UpdateMediaManagement(context.TransactionOutput, ct);
            log.Information("Media management has been updated");
            log.Debug("Media management differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media management is up to date!");
        }

        context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
        return PipelineFlow.Continue;
    }
}
