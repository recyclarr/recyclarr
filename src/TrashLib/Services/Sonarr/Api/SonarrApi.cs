using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Config.Services;
using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public class SonarrApi : ISonarrApi
{
    private readonly ISonarrReleaseProfileCompatibilityHandler _profileHandler;
    private readonly IServerInfo _serverInfo;

    public SonarrApi(
        IServerInfo serverInfo,
        ISonarrReleaseProfileCompatibilityHandler profileHandler)
    {
        _serverInfo = serverInfo;
        _profileHandler = profileHandler;
    }

    public async Task<IList<SonarrTag>> GetTags()
    {
        return await BaseUrl()
            .AppendPathSegment("tag")
            .GetJsonAsync<List<SonarrTag>>();
    }

    public async Task<SonarrTag> CreateTag(string tag)
    {
        return await BaseUrl()
            .AppendPathSegment("tag")
            .PostJsonAsync(new {label = tag})
            .ReceiveJson<SonarrTag>();
    }

    public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles()
    {
        var response = await BaseUrl()
            .AppendPathSegment("releaseprofile")
            .GetJsonAsync<List<JObject>>();

        return response
            .Select(_profileHandler.CompatibleReleaseProfileForReceiving)
            .ToList();
    }

    public async Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSendingAsync(profileToUpdate);
        await BaseUrl()
            .AppendPathSegment($"releaseprofile/{profileToUpdate.Id}")
            .PutJsonAsync(profileToSend);
    }

    public async Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile)
    {
        var profileToSend = await _profileHandler.CompatibleReleaseProfileForSendingAsync(newProfile);
        var response = await BaseUrl()
            .AppendPathSegment("releaseprofile")
            .PostJsonAsync(profileToSend)
            .ReceiveJson<JObject>();

        return _profileHandler.CompatibleReleaseProfileForReceiving(response);
    }

    public async Task DeleteReleaseProfile(int releaseProfileId)
    {
        await BaseUrl()
            .AppendPathSegment($"releaseprofile/{releaseProfileId}")
            .DeleteAsync();
    }

    public async Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition()
    {
        return await BaseUrl()
            .AppendPathSegment("qualitydefinition")
            .GetJsonAsync<List<SonarrQualityDefinitionItem>>();
    }

    public async Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
        IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality)
    {
        return await BaseUrl()
            .AppendPathSegment("qualityDefinition/update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<SonarrQualityDefinitionItem>>();
    }

    private Url BaseUrl() => _serverInfo.BuildRequest();
}
