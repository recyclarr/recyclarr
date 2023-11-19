using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiPersistencePhase(IMediaNamingApiService api)
{
    public async Task Execute(IServiceConfiguration config, MediaNamingDto serviceDto)
    {
        await api.UpdateNaming(config, serviceDto);
    }
}
