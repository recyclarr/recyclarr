using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.PipelinePhases;

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
