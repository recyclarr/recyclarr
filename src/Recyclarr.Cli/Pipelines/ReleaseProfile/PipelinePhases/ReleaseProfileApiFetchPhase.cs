using Recyclarr.Cli.Pipelines.ReleaseProfile.Api;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.TrashLib.Config;

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
