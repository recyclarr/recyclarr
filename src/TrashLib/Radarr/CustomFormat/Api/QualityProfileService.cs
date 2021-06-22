using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace TrashLib.Radarr.CustomFormat.Api
{
    internal class QualityProfileService : IQualityProfileService
    {
        private readonly string _baseUrl;

        public QualityProfileService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public async Task<List<JObject>> GetQualityProfiles()
        {
            return await _baseUrl
                .AppendPathSegment("qualityprofile")
                .GetJsonAsync<List<JObject>>();
        }

        public async Task<JObject> UpdateQualityProfile(JObject profileJson, int id)
        {
            return await _baseUrl
                .AppendPathSegment($"qualityprofile/{id}")
                .PutJsonAsync(profileJson)
                .ReceiveJson<JObject>();
        }
    }
}
