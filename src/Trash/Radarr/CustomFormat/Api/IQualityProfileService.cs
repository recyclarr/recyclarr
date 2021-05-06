using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Trash.Radarr.CustomFormat.Api
{
    public interface IQualityProfileService
    {
        Task<List<JObject>> GetQualityProfiles();
        Task<JObject> UpdateQualityProfile(JObject profileJson, int id);
    }
}
