using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiFetchPhase(IReleaseProfileApiService rpService)
    : IApiFetchPipelinePhase<ReleaseProfilePipelineContext>
{
    public async Task Execute(ReleaseProfilePipelineContext context, IServiceConfiguration config)
    {
        context.ApiFetchOutput = await rpService.GetReleaseProfiles(config);
    }
}
