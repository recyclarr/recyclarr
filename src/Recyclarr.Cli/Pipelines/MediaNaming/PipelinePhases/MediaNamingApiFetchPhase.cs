using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiFetchPhase
{
    private readonly IMediaNamingApiService _api;

    public MediaNamingApiFetchPhase(IMediaNamingApiService api)
    {
        _api = api;
    }

    public async Task<MediaNamingDto> Execute(IServiceConfiguration config)
    {
        return await _api.GetNaming(config);
    }
}
