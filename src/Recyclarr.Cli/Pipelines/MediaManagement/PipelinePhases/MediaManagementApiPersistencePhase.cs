using Recyclarr.ServarrApi.MediaManagement;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementApiPersistencePhase(
    ILogger log,
    IMediaManagementApiService api,
    IProgressSource progressSource
) : IPipelinePhase<MediaManagementPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        MediaManagementPipelineContext context,
        CancellationToken ct
    )
    {
        await api.UpdateMediaManagement(context.TransactionOutput, ct);
        LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }

    private void LogPersistenceResults(MediaManagementPipelineContext context)
    {
        var differences = context.ApiFetchOutput.GetDifferences(context.TransactionOutput);

        if (differences.Count != 0)
        {
            log.Information("Media management has been updated");
            log.Debug("Media management differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media management is up to date!");
        }

        progressSource.SetPipelineStatus(PipelineProgressStatus.Succeeded, differences.Count);
    }
}
