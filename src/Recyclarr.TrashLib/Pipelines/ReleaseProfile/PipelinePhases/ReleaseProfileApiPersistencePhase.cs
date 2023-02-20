using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Models;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.PipelinePhases;

public class ReleaseProfileApiPersistencePhase
{
    private readonly ILogger _log;
    private readonly IReleaseProfileApiService _api;

    public ReleaseProfileApiPersistencePhase(ILogger log, IReleaseProfileApiService api)
    {
        _log = log;
        _api = api;
    }

    public async Task Execute(IServiceConfiguration config, ReleaseProfileTransactionData transactions)
    {
        foreach (var profile in transactions.UpdatedProfiles)
        {
            _log.Information("Update existing profile: {ProfileName}", profile.Name);
            await _api.UpdateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.CreatedProfiles)
        {
            _log.Information("Create new profile: {ProfileName}", profile.Name);
            await _api.CreateReleaseProfile(config, profile);
        }

        foreach (var profile in transactions.DeletedProfiles)
        {
            _log.Information("Deleting old release profile: {ProfileName}", profile.Name);
            await _api.DeleteReleaseProfile(config, profile.Id);
        }
    }
}
