using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiPersistencePhase(IReleaseProfileApiService api)
    : IApiPersistencePipelinePhase<ReleaseProfilePipelineContext>
{
    public async Task Execute(ReleaseProfilePipelineContext context, IServiceConfiguration config)
    {
        var transactions = context.TransactionOutput;

        foreach (var profile in transactions.UpdatedProfiles)
        {
            await api.UpdateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.CreatedProfiles)
        {
            await api.CreateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.DeletedProfiles)
        {
            await api.DeleteReleaseProfile(config, profile.Id);
        }
    }
}
