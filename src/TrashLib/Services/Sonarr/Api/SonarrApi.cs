using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public class SonarrApi : ISonarrApi
{
    private readonly IServiceRequestBuilder _service;

    public SonarrApi(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<IList<SonarrTag>> GetTags()
    {
        return await _service.Request("tag")
            .GetJsonAsync<List<SonarrTag>>();
    }

    public async Task<SonarrTag> CreateTag(string tag)
    {
        return await _service.Request("tag")
            .PostJsonAsync(new {label = tag})
            .ReceiveJson<SonarrTag>();
    }

    public async Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition()
    {
        return await _service.Request("qualitydefinition")
            .GetJsonAsync<List<SonarrQualityDefinitionItem>>();
    }

    public async Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
        IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality)
    {
        return await _service.Request("qualityDefinition", "update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<SonarrQualityDefinitionItem>>();
    }
}
