using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiFetchPhase(IReleaseProfileApiService rpService)
{
    public async Task<IList<SonarrReleaseProfile>> Execute(IServiceConfiguration config)
    {
        return await rpService.GetReleaseProfiles(config);
    }
}
