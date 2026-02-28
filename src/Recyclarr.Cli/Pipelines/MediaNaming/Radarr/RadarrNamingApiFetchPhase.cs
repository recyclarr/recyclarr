using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingApiFetchPhase(IRadarrMediaNamingApiService api)
    : IPipelinePhase<RadarrNamingPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        RadarrNamingPipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetNaming(ct);
        return PipelineFlow.Continue;
    }
}
