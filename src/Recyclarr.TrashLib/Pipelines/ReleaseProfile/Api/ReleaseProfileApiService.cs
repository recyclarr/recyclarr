using Flurl.Http;
using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api;

public class ReleaseProfileApiService : IReleaseProfileApiService
{
    private readonly ISonarrReleaseProfileCompatibilityHandler _profileHandler;
    private readonly IServiceRequestBuilder _service;

    public ReleaseProfileApiService(
        ISonarrReleaseProfileCompatibilityHandler profileHandler,
        IServiceRequestBuilder service)
    {
        _profileHandler = profileHandler;
        _service = service;
    }

    public async Task UpdateReleaseProfile(IServiceConfiguration config, SonarrReleaseProfile profile)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSending(config, profile);
        await _service.Request(config, "releaseprofile", profile.Id)
            .PutJsonAsync(profileToSend);
    }

    public async Task<SonarrReleaseProfile> CreateReleaseProfile(
        IServiceConfiguration config,
        SonarrReleaseProfile profile)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSending(config, profile);

        var response = await _service.Request(config, "releaseprofile")
            .PostJsonAsync(profileToSend)
            .ReceiveJson<JObject>();

        return _profileHandler.CompatibleReleaseProfileForReceiving(response);
    }

    public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles(IServiceConfiguration config)
    {
        var response = await _service.Request(config, "releaseprofile")
            .GetJsonAsync<List<JObject>>();

        return response
            .Select(_profileHandler.CompatibleReleaseProfileForReceiving)
            .ToList();
    }

    public async Task DeleteReleaseProfile(IServiceConfiguration config, int releaseProfileId)
    {
        await _service.Request(config, "releaseprofile", releaseProfileId)
            .DeleteAsync();
    }
}
