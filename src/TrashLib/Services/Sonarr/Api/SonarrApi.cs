using Flurl;
using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Services.Sonarr.Api.Objects;

namespace TrashLib.Services.Sonarr.Api;

public class SonarrApi : ISonarrApi
{
    private readonly IServerInfo _serverInfo;

    public SonarrApi(IServerInfo serverInfo)
    {
        _serverInfo = serverInfo;
    }

    public async Task<Version> GetVersion()
    {
        var response = await BaseUrl()
            .AppendPathSegment("system/status")
            .GetJsonAsync();

        return new Version(response.version);
    }

    public async Task<IList<SonarrTag>> GetTags()
    {
        return await BaseUrl()
            .AppendPathSegment("tag")
            .GetJsonAsync<List<SonarrTag>>();
    }

    public async Task<SonarrTag> CreateTag(string tag)
    {
        return await BaseUrl()
            .AppendPathSegment("tag")
            .PostJsonAsync(new {label = tag})
            .ReceiveJson<SonarrTag>();
    }

    public async Task<IReadOnlyCollection<SonarrQualityDefinitionItem>> GetQualityDefinition()
    {
        return await BaseUrl()
            .AppendPathSegment("qualitydefinition")
            .GetJsonAsync<List<SonarrQualityDefinitionItem>>();
    }

    public async Task<IList<SonarrQualityDefinitionItem>> UpdateQualityDefinition(
        IReadOnlyCollection<SonarrQualityDefinitionItem> newQuality)
    {
        return await BaseUrl()
            .AppendPathSegment("qualityDefinition/update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<SonarrQualityDefinitionItem>>();
    }

    private Url BaseUrl() => _serverInfo.BuildRequest();
}
