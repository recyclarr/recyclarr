using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingApiFetchPhase(ISonarrMediaNamingApiService api)
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
