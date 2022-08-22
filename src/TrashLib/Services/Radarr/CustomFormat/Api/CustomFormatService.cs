using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Config.Services;
using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Api;

internal class CustomFormatService : ICustomFormatService
{
    private readonly IServerInfo _serverInfo;

    public CustomFormatService(IServerInfo serverInfo)
    {
        _serverInfo = serverInfo;
    }

    public async Task<List<JObject>> GetCustomFormats()
    {
        return await BuildRequest()
            .AppendPathSegment("customformat")
            .GetJsonAsync<List<JObject>>();
    }

    public async Task CreateCustomFormat(ProcessedCustomFormatData cf)
    {
        var response = await BuildRequest()
            .AppendPathSegment("customformat")
            .PostJsonAsync(cf.Json)
            .ReceiveJson<JObject>();

        if (response != null)
        {
            cf.SetCache(response.Value<int>("id"));
        }
    }

    public async Task UpdateCustomFormat(ProcessedCustomFormatData cf)
    {
        await BuildRequest()
            .AppendPathSegment($"customformat/{cf.GetCustomFormatId()}")
            .PutJsonAsync(cf.Json)
            .ReceiveJson<JObject>();
    }

    public async Task DeleteCustomFormat(int customFormatId)
    {
        await BuildRequest()
            .AppendPathSegment($"customformat/{customFormatId}")
            .DeleteAsync();
    }

    private Url BuildRequest() => _serverInfo.BuildRequest();
}
