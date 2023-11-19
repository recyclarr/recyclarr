using Recyclarr.Cli.Pipelines.ReleaseProfile.Models;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiPersistencePhase(ILogger log, IReleaseProfileApiService api)
{
    public async Task Execute(IServiceConfiguration config, ReleaseProfileTransactionData transactions)
    {
        foreach (var profile in transactions.UpdatedProfiles)
        {
            log.Information("Update existing profile: {ProfileName}", profile.Name);
            await api.UpdateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.CreatedProfiles)
        {
            log.Information("Create new profile: {ProfileName}", profile.Name);
            await api.CreateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.DeletedProfiles)
        {
            log.Information("Deleting old release profile: {ProfileName}", profile.Name);
            await api.DeleteReleaseProfile(config, profile.Id);
        }
    }
}
