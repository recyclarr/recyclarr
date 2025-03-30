using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

internal class MediaNamingApiFetchPhase(IMediaNamingApiService api)
    : IPipelinePhase<MediaNamingPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        MediaNamingPipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetNaming(ct);
        return PipelineFlow.Continue;
    }
}
