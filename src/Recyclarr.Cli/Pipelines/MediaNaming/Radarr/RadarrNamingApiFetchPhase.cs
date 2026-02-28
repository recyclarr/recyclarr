using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingApiFetchPhase(IRadarrNamingService api)
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
