using Flurl.Http;
using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

internal class QualityProfileService : IQualityProfileService
{
    private readonly IServiceRequestBuilder _service;

    public QualityProfileService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<List<JObject>> GetQualityProfiles(IServiceConfiguration config)
    {
        return await _service.Request(config, "qualityprofile")
            .GetJsonAsync<List<JObject>>();
    }

    public async Task<JObject> UpdateQualityProfile(IServiceConfiguration config, JObject profileJson, int id)
    {
        return await _service.Request(config, "qualityprofile", id)
            .PutJsonAsync(profileJson)
            .ReceiveJson<JObject>();
    }
}
