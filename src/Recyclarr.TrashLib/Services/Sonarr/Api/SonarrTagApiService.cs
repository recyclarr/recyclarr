using Flurl.Http;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public class SonarrTagApiService : ISonarrTagApiService
{
    private readonly IServiceRequestBuilder _service;

    public SonarrTagApiService(IServiceRequestBuilder service)
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
}
