using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.QualitySize.Api;

internal class QualityDefinitionService : IQualityDefinitionService
{
    private readonly IServiceRequestBuilder _service;

    public QualityDefinitionService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<List<ServiceQualityDefinitionItem>> GetQualityDefinition()
    {
        return await _service.Request("qualitydefinition")
            .GetJsonAsync<List<ServiceQualityDefinitionItem>>();
    }

    public async Task<IList<ServiceQualityDefinitionItem>> UpdateQualityDefinition(
        IList<ServiceQualityDefinitionItem> newQuality)
    {
        return await _service.Request("qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<ServiceQualityDefinitionItem>>();
    }
}
