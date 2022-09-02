using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Config.Services;
using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public class ReleaseProfileApiService : IReleaseProfileApiService
{
    private readonly ISonarrReleaseProfileCompatibilityHandler _profileHandler;
    private readonly IServerInfo _serverInfo;

    public ReleaseProfileApiService(
        ISonarrReleaseProfileCompatibilityHandler profileHandler,
        IServerInfo serverInfo)
    {
        _profileHandler = profileHandler;
        _serverInfo = serverInfo;
    }

    public async Task UpdateReleaseProfile(SonarrReleaseProfile profile)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSendingAsync(profile);
        await _serverInfo.BuildRequest()
            .AppendPathSegment($"releaseprofile/{profile.Id}")
            .PutJsonAsync(profileToSend);
    }

    public async Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile profile)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSendingAsync(profile);

        var response = await _serverInfo.BuildRequest()
            .AppendPathSegment("releaseprofile")
            .PostJsonAsync(profileToSend)
            .ReceiveJson<JObject>();

        return _profileHandler.CompatibleReleaseProfileForReceiving(response);
    }

    public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles()
    {
        var response = await _serverInfo.BuildRequest()
            .AppendPathSegment("releaseprofile")
            .GetJsonAsync<List<JObject>>();

        return response
            .Select(_profileHandler.CompatibleReleaseProfileForReceiving)
            .ToList();
    }

    public async Task DeleteReleaseProfile(int releaseProfileId)
    {
        await _serverInfo.BuildRequest()
            .AppendPathSegment($"releaseprofile/{releaseProfileId}")
            .DeleteAsync();
    }
}
