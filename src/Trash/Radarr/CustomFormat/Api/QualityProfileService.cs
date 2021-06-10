using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Trash.Config;

namespace Trash.Radarr.CustomFormat.Api
{
    internal class QualityProfileService : IQualityProfileService
    {
        private readonly IServerInfo _serverInfo;

        public QualityProfileService(IServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
        }

        private string BaseUrl => _serverInfo.BuildUrl();

        public async Task<List<JObject>> GetQualityProfiles()
        {
            return await BaseUrl
                .AppendPathSegment("qualityprofile")
                .GetJsonAsync<List<JObject>>();
        }

        public async Task<JObject> UpdateQualityProfile(JObject profileJson, int id)
        {
            return await BaseUrl
                .AppendPathSegment($"qualityprofile/{id}")
                .PutJsonAsync(profileJson)
                .ReceiveJson<JObject>();
        }
    }
}
