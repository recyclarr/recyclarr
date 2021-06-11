using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    internal class CustomFormatService : ICustomFormatService
    {
        private readonly IServerInfo _serverInfo;

        public CustomFormatService(IServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
        }

        private string BaseUrl => _serverInfo.BuildUrl();

        public async Task<List<JObject>> GetCustomFormats()
        {
            return await BaseUrl
                .AppendPathSegment("customformat")
                .GetJsonAsync<List<JObject>>();
        }

        public async Task CreateCustomFormat(ProcessedCustomFormatData cf)
        {
            var response = await BaseUrl
                .AppendPathSegment("customformat")
                .PostJsonAsync(cf.Json)
                .ReceiveJson<JObject>();

            cf.SetCache((int) response["id"]);
        }

        public async Task UpdateCustomFormat(ProcessedCustomFormatData cf)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{cf.GetCustomFormatId()}")
                .PutJsonAsync(cf.Json)
                .ReceiveJson<JObject>();
        }

        public async Task DeleteCustomFormat(int customFormatId)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{customFormatId}")
                .DeleteAsync();
        }
    }
}
