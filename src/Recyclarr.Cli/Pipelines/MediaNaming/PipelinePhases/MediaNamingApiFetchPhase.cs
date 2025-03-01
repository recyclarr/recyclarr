using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

internal class MediaNamingApiFetchPhase(IMediaNamingApiService api)
    : IPipelinePhase<MediaNamingPipelineContext>
{
    public async Task<bool> Execute(MediaNamingPipelineContext context, CancellationToken ct)
    {
        context.ApiFetchOutput = await api.GetNaming(ct);
        return true;
    }
}
