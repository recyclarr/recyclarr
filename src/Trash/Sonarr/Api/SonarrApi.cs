using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Trash.Config;
using Trash.Sonarr.Api.Objects;

namespace Trash.Sonarr.Api
{
    public class SonarrApi : ISonarrApi
    {
        private readonly IConfigurationProvider<SonarrConfiguration> _config;

        public SonarrApi(IConfigurationProvider<SonarrConfiguration> config)
        {
            _config = config;
        }

        private string BaseUrl()
        {
            if (_config.ActiveConfiguration == null)
            {
                throw new InvalidOperationException("No active configuration available for API method");
            }

            return _config.ActiveConfiguration.BuildUrl();
        }

        public async Task<Version> GetVersion()
        {
            dynamic data = await BaseUrl()
                .AppendPathSegment("system/status")
                .GetJsonAsync();
            return new Version(data.version);
        }

        public async Task<List<SonarrTag>> GetTags()
        {
            return await BaseUrl()
                .AppendPathSegment("tag")
                .GetJsonAsync<List<SonarrTag>>();
        }

        public async Task<SonarrTag> CreateTag(string tag)
        {
            return await BaseUrl()
                .AppendPathSegment("tag")
                .PostJsonAsync(new {label = tag})
                .ReceiveJson<SonarrTag>();
        }

        public async Task<List<SonarrReleaseProfile>> GetReleaseProfiles()
        {
            return await BaseUrl()
                .AppendPathSegment("releaseprofile")
                .GetJsonAsync<List<SonarrReleaseProfile>>();
        }

        public async Task UpdateReleaseProfile(SonarrReleaseProfile profileToUpdate)
        {
            await BaseUrl()
                .AppendPathSegment($"releaseprofile/{profileToUpdate.Id}")
                .PutJsonAsync(profileToUpdate);
        }

        public async Task<SonarrReleaseProfile> CreateReleaseProfile(SonarrReleaseProfile newProfile)
        {
            return await BaseUrl()
                .AppendPathSegment("releaseprofile")
                .PostJsonAsync(newProfile)
                .ReceiveJson<SonarrReleaseProfile>();
        }

        public async Task<List<SonarrQualityDefinitionItem>> GetQualityDefinition()
        {
            return await BaseUrl()
                .AppendPathSegment("qualitydefinition")
                .GetJsonAsync<List<SonarrQualityDefinitionItem>>();
        }

        public async Task<List<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
            List<SonarrQualityDefinitionItem> newQuality)
        {
            return await BaseUrl()
                .AppendPathSegment("qualityDefinition/update")
                .PutJsonAsync(newQuality)
                .ReceiveJson<List<SonarrQualityDefinitionItem>>();
        }
    }
}
