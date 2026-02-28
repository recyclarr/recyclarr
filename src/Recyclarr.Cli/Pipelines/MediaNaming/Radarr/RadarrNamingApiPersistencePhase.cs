using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingApiPersistencePhase(ILogger log, IRadarrNamingService api)
    : IPipelinePhase<RadarrNamingPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        RadarrNamingPipelineContext context,
        CancellationToken ct
    )
    {
        await api.UpdateNaming(context.TransactionOutput, ct);

        var differences = context.ApiFetchOutput.GetDifferences(context.TransactionOutput);

        if (differences.Count != 0)
        {
            log.Information("Media naming has been updated");
            log.Debug("Naming differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media naming is up to date!");
        }

        context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
        return PipelineFlow.Continue;
    }
}
