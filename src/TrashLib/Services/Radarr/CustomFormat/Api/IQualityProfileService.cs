using Newtonsoft.Json.Linq;

namespace TrashLib.Services.Radarr.CustomFormat.Api;

public interface IQualityProfileService
{
    Task<List<JObject>> GetQualityProfiles();
    Task<JObject> UpdateQualityProfile(JObject profileJson, int id);
}
