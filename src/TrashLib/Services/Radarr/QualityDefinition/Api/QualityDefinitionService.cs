using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Services.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Services.Radarr.QualityDefinition.Api;

internal class QualityDefinitionService : IQualityDefinitionService
{
    private readonly IServiceRequestBuilder _service;

    public QualityDefinitionService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition()
    {
        return await _service.Request("qualitydefinition")
            .GetJsonAsync<List<RadarrQualityDefinitionItem>>();
    }

    public async Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(
        IList<RadarrQualityDefinitionItem> newQuality)
    {
        return await _service.Request("qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<RadarrQualityDefinitionItem>>();
    }
}
