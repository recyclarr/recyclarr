using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http;

namespace Recyclarr.ServarrApi.QualityDefinition;

internal class QualityDefinitionApiService : IQualityDefinitionApiService
{
    private readonly IServarrRequestBuilder _service;

    public QualityDefinitionApiService(IServarrRequestBuilder service)
    {
        _service = service;
    }

    public async Task<IList<ServiceQualityDefinitionItem>> GetQualityDefinition(IServiceConfiguration config)
    {
        return await _service.Request(config, "qualitydefinition")
            .GetJsonAsync<List<ServiceQualityDefinitionItem>>();
    }

    public async Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IServiceConfiguration config,
        IList<ServiceQualityDefinitionItem> newQuality)
    {
        return await _service.Request(config, "qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<ServiceQualityDefinitionItem>>();
    }
}
