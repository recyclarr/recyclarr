using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Trash.Config;
using Trash.Radarr.Api.Objects;

namespace Trash.Radarr.Api
{
    public class RadarrApi : IRadarrApi
    {
        private readonly IConfigurationProvider<RadarrConfiguration> _config;

        public RadarrApi(IConfigurationProvider<RadarrConfiguration> config)
        {
            _config = config;
        }

        public async Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition()
        {
            return await BaseUrl()
                .AppendPathSegment("qualitydefinition")
                .GetJsonAsync<List<RadarrQualityDefinitionItem>>();
        }

        public async Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(
            IList<RadarrQualityDefinitionItem> newQuality)
        {
            return await BaseUrl()
                .AppendPathSegment("qualityDefinition/update")
                .PutJsonAsync(newQuality)
                .ReceiveJson<List<RadarrQualityDefinitionItem>>();
        }

        private string BaseUrl()
        {
            if (_config.ActiveConfiguration == null)
            {
                throw new InvalidOperationException("No active configuration available for API method");
            }

            return _config.ActiveConfiguration.BuildUrl();
        }
    }
}
