using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Config;
using TrashLib.Config.Services;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Api;

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

    private IFlurlRequest BuildRequest() => _serverInfo.BuildRequest();
}
