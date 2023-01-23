using Flurl.Http;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;

namespace Recyclarr.TrashLib.Pipelines.Tags.Api;

public class SonarrTagApiService : ISonarrTagApiService
{
    private readonly IServiceRequestBuilder _service;

    public SonarrTagApiService(IServiceRequestBuilder service)
    {
        _service = service;
    }

    public async Task<IList<SonarrTag>> GetTags(IServiceConfiguration config)
    {
        return await _service.Request(config, "tag")
            .GetJsonAsync<List<SonarrTag>>();
    }

    public async Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag)
    {
        return await _service.Request(config, "tag")
            .PostJsonAsync(new {label = tag})
            .ReceiveJson<SonarrTag>();
    }
}
