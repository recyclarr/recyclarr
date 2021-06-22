using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using TrashLib.Sonarr.Api.Objects;

namespace TrashLib.Sonarr.Api
{
    public class SonarrApi : ISonarrApi
    {
        private readonly string _baseUrl;

        public SonarrApi(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public async Task<Version> GetVersion()
        {
            dynamic data = await _baseUrl
                .AppendPathSegment("system/status")
                .GetJsonAsync();
            return new Version(data.version);
        }

        public async Task<IList<SonarrTag>> GetTags()
        {
            return await _baseUrl
                .AppendPathSegment("tag")
                .GetJsonAsync<List<SonarrTag>>();
        }

        public async Task<SonarrTag> CreateTag(string tag)
        {
            return await _baseUrl
                .AppendPathSegment("tag")
                .PostJsonAsync(new {label = tag})
                .ReceiveJson<SonarrTag>();
        }

        public async Task<IList<SonarrReleaseProfile>> GetReleaseProfiles()
        {
            return await _baseUrl
                .AppendPathSegment("releaseprofile")
                .GetJsonAsync<List<SonarrReleaseProfile>>();
        }

        public async Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate)
        {
            await _baseUrl
                .AppendPathSegment($"releaseprofile/{profileToUpdate.Id}")
                .PutJsonAsync(profileToUpdate);
        }

        public async Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile)
        {
            return await _baseUrl
                .AppendPathSegment("releaseprofile")
                .PostJsonAsync(newProfile)
                .ReceiveJson<SonarrReleaseProfile>();
        }

        public async Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition()
        {
            return await _baseUrl
                .AppendPathSegment("qualitydefinition")
                .GetJsonAsync<List<SonarrQualityDefinitionItem>>();
        }

        public async Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
            IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality)
        {
            return await _baseUrl
                .AppendPathSegment("qualityDefinition/update")
                .PutJsonAsync(newQuality)
                .ReceiveJson<List<SonarrQualityDefinitionItem>>();
        }
    }
}
