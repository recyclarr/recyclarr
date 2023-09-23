using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.Cli.Pipelines.QualitySize.Api;

internal class QualityDefinitionService : IQualityDefinitionService
{
    private readonly IServiceRequestBuilder _service;

    public QualityDefinitionService(IServiceRequestBuilder service)
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
