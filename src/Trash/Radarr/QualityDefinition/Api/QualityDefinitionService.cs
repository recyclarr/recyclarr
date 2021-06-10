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
        private readonly IServerInfo _serverInfo;

        public QualityDefinitionService(IServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
        }

        private string BaseUrl => _serverInfo.BuildUrl();

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
