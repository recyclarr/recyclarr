using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiFetchPhase
{
    private readonly IReleaseProfileApiService _rpService;

    public ReleaseProfileApiFetchPhase(
        IReleaseProfileApiService rpService)
    {
        _rpService = rpService;
    }

    public async Task<IList<SonarrReleaseProfile>> Execute(IServiceConfiguration config)
    {
        return await _rpService.GetReleaseProfiles(config);
    }
}
