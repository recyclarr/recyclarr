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
        public CustomFormatService(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        private string BaseUrl { get; }

        public async Task<List<JObject>> GetCustomFormats()
        {
            return await BaseUrl
                .AppendPathSegment("customformat")
                .GetJsonAsync<List<JObject>>();
        }

        public async Task<int> CreateCustomFormat(ProcessedCustomFormatData cf)
        {
            var response = await BaseUrl
                .AppendPathSegment("customformat")
                .PostJsonAsync(cf.Json)
                .ReceiveJson<JObject>();

            return (int) response["id"];
        }

        public async Task UpdateCustomFormat(int formatId, ProcessedCustomFormatData cf)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{formatId}")
                .PutJsonAsync(cf.Json);
        }

        public async Task DeleteCustomFormat(int formatId)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{formatId}")
                .DeleteAsync();
        }
    }
}
