using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Config.Services;

namespace TrashLib.Services.QualityProfile.Api;

internal class QualityProfileService : IQualityProfileService
{
    private readonly IServerInfo _serverInfo;

    public QualityProfileService(IServerInfo serverInfo)
    {
        _serverInfo = serverInfo;
    }

    public async Task<List<JObject>> GetQualityProfiles()
    {
        return await BuildRequest()
            .AppendPathSegment("qualityprofile")
            .GetJsonAsync<List<JObject>>();
    }

    public async Task<JObject> UpdateQualityProfile(JObject profileJson, int id)
    {
        return await BuildRequest()
            .AppendPathSegment($"qualityprofile/{id}")
            .PutJsonAsync(profileJson)
            .ReceiveJson<JObject>();
    }

    private Url BuildRequest() => _serverInfo.BuildRequest();
}
