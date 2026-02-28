using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingApiFetchPhase(ISonarrNamingService api)
    : IPipelinePhase<SonarrNamingPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        SonarrNamingPipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetNaming(ct);
        return PipelineFlow.Continue;
    }
}
