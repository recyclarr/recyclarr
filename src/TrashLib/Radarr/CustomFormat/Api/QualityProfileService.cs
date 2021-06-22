using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using TrashLib.Radarr.CustomFormat.Api.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    internal class QualityProfileService : IQualityProfileService
    {
        private readonly string _baseUrl;

        public QualityProfileService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public async Task<List<QualityProfileData>> GetQualityProfiles()
        {
            return await _baseUrl
                .AppendPathSegment("qualityprofile")
                .GetJsonAsync<List<QualityProfileData>>();
        }

        public async Task<QualityProfileData> UpdateQualityProfile(QualityProfileData profile, int profileId)
        {
            return await _baseUrl
                .AppendPathSegment($"qualityprofile/{profileId}")
                .PutJsonAsync(profile)
                .ReceiveJson<QualityProfileData>();
        }
    }
}
