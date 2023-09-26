using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiPersistencePhase
{
    private readonly IMediaNamingApiService _api;

    public MediaNamingApiPersistencePhase(IMediaNamingApiService api)
    {
        _api = api;
    }

    public async Task Execute(IServiceConfiguration config, MediaNamingDto serviceDto)
    {
        await _api.UpdateNaming(config, serviceDto);
    }
}
