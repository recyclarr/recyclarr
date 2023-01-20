using Newtonsoft.Json.Linq;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.CustomFormat.Api;

public interface IQualityProfileService
{
    Task<List<JObject>> GetQualityProfiles(IServiceConfiguration config);
    Task<JObject> UpdateQualityProfile(IServiceConfiguration config, JObject profileJson, int id);
}
