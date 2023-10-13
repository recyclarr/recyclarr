using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiPersistencePhase(IMediaNamingApiService api)
    : IApiPersistencePipelinePhase<MediaNamingPipelineContext>
{
    public async Task Execute(MediaNamingPipelineContext context, IServiceConfiguration config)
    {
        await api.UpdateNaming(config, context.TransactionOutput);
    }
}
