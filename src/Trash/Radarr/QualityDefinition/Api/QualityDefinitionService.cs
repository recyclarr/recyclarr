using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Trash.Config;
using Trash.Radarr.QualityDefinition.Api.Objects;

namespace Trash.Radarr.QualityDefinition.Api
{
    public class QualityDefinitionService : IQualityDefinitionService
    {
        private readonly IServiceConfiguration _serviceConfig;

        public QualityDefinitionService(IServiceConfiguration serviceConfig)
        {
            _serviceConfig = serviceConfig;
        }

        private string BaseUrl => _serviceConfig.BuildUrl();

        public async Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition()
        {
            return await BaseUrl
                .AppendPathSegment("qualitydefinition")
                .GetJsonAsync<List<RadarrQualityDefinitionItem>>();
        }

        public async Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(
            IList<RadarrQualityDefinitionItem> newQuality)
        {
            return await BaseUrl
                .AppendPathSegment("qualityDefinition/update")
                .PutJsonAsync(newQuality)
                .ReceiveJson<List<RadarrQualityDefinitionItem>>();
        }
    }
}
