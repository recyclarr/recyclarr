using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using TrashLib.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Radarr.QualityDefinition.Api
{
    internal class QualityDefinitionService : IQualityDefinitionService
    {
        private readonly string _baseUrl;

        public QualityDefinitionService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public async Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition()
        {
            return await _baseUrl
                .AppendPathSegment("qualitydefinition")
                .GetJsonAsync<List<RadarrQualityDefinitionItem>>();
        }

        public async Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(
            IList<RadarrQualityDefinitionItem> newQuality)
        {
            return await _baseUrl
                .AppendPathSegment("qualityDefinition/update")
                .PutJsonAsync(newQuality)
                .ReceiveJson<List<RadarrQualityDefinitionItem>>();
        }
    }
}
