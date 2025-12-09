using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

internal class MediaNamingApiPersistencePhase(
    ILogger log,
    IMediaNamingApiService api,
    ISyncEventPublisher eventPublisher
) : IPipelinePhase<MediaNamingPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        MediaNamingPipelineContext context,
        CancellationToken ct
    )
    {
        await api.UpdateNaming(context.TransactionOutput, ct);
        LogPersistenceResults(context);
        return PipelineFlow.Continue;
    }

    private void LogPersistenceResults(MediaNamingPipelineContext context)
    {
        var differences = context.ApiFetchOutput switch
        {
            RadarrMediaNamingDto x => x.GetDifferences(context.TransactionOutput),
            SonarrMediaNamingDto x => x.GetDifferences(context.TransactionOutput),
            _ => throw new ArgumentException("Unsupported configuration type"),
        };

        if (differences.Count != 0)
        {
            log.Information("Media naming has been updated");
            log.Debug("Naming differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media naming is up to date!");
        }

        eventPublisher.AddCompletionCount(differences.Count != 0 ? 1 : 0);
    }
}
