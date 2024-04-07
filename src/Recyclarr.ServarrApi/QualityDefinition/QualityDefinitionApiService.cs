using Flurl.Http;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class QualityDefinitionApiService(IServarrRequestBuilder service) : IQualityDefinitionApiService
{
    public async Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config)
    {
        return await service.Request(config, "qualitydefinition")
            .GetJsonAsync<List<ServiceQualityDefinitionItem>>();
    }

    public async Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality)
    {
        return await service.Request(config, "qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<ServiceQualityDefinitionItem>>();
    }
}
