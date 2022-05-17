using Flurl;
using Flurl.Http;
using TrashLib.Config.Services;
using TrashLib.Radarr.QualityDefinition.Api.Objects;

namespace TrashLib.Radarr.QualityDefinition.Api;

internal class QualityDefinitionService : IQualityDefinitionService
{
    private readonly IServerInfo _serverInfo;

    public QualityDefinitionService(IServerInfo serverInfo)
    {
        _serverInfo = serverInfo;
    }

    public async Task<List<RadarrQualityDefinitionItem>> GetQualityDefinition()
    {
        return await BuildRequest()
            .AppendPathSegment("qualitydefinition")
            .GetJsonAsync<List<RadarrQualityDefinitionItem>>();
    }

    public async Task<IList<RadarrQualityDefinitionItem>> UpdateQualityDefinition(
        IList<RadarrQualityDefinitionItem> newQuality)
    {
        return await BuildRequest()
            .AppendPathSegment("qualityDefinition/update")
            .PutJsonAsync(newQuality)
            .ReceiveJson<List<RadarrQualityDefinitionItem>>();
    }

    private Url BuildRequest() => _serverInfo.BuildRequest();
}
