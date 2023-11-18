using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public class MediaNamingApiFetchPhase(IMediaNamingApiService api)
{
    public async Task<MediaNamingDto> Execute(IServiceConfiguration config)
    {
        return await api.GetNaming(config);
    }
}
