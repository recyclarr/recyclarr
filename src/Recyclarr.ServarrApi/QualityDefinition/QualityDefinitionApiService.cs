using Flurl.Http;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class QualityDefinitionApiService(IServarrRequestBuilder service) : IQualityDefinitionApiService
{
    public async Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition()
    {
        return await service.Request("qualitydefinition")
            .GetJsonAsync<List<ServiceQualityDefinitionItem>>();
    }

    public async Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IList<ServiceQualityDefinitionItem> newQuality)
    {
        return await service.Request("qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<ServiceQualityDefinitionItem>>();
    }
}
