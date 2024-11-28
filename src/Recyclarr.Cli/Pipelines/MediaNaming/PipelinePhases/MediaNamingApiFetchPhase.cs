using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiFetchPhase(IMediaNamingApiService api)
    : IApiFetchPipelinePhase<MediaNamingPipelineContext>
{
    public async Task Execute(MediaNamingPipelineContext context, CancellationToken ct)
    {
        context.ApiFetchOutput = await api.GetNaming(ct);
    }
}
