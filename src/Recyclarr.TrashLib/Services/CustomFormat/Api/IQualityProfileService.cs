using Newtonsoft.Json.Linq;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

public interface IQualityProfileService
{
    Task<List<JObject>> GetQualityProfiles();
    Task<JObject> UpdateQualityProfile(JObject profileJson, int id);
}
