using Flurl.Http;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Http.Servarr;

namespace Recyclarr.ServarrApi.Tag;

public class SonarrTagApiService(IServarrRequestBuilder service) : ISonarrTagApiService
{
    public async Task<IList<SonarrTag>> GetTags(IServiceConfiguration config)
    {
        return await service.Request(config, "tag")
            .GetJsonAsync<List<SonarrTag>>();
    }

    public async Task<SonarrTag> CreateTag(IServiceConfiguration config, string tag)
    {
        return await service.Request(config, "tag")
            .PostJsonAsync(new {label = tag})
            .ReceiveJson<SonarrTag>();
    }
}
