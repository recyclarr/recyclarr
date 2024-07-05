using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiPersistencePhase(IMediaNamingApiService api)
    : IApiPersistencePipelinePhase<MediaNamingPipelineContext>
{
    public async Task Execute(MediaNamingPipelineContext context, CancellationToken ct)
    {
        await api.UpdateNaming(context.TransactionOutput, ct);
    }
}
