using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Api.Models;
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

        public async Task<List<CustomFormatData>> GetCustomFormats()
        {
            return await BaseUrl
                .AppendPathSegment("customformat")
                .GetJsonAsync<List<CustomFormatData>>();
        }

        public async Task<int> CreateCustomFormat(ProcessedCustomFormatData cf)
        {
            var response = await BaseUrl
                .AppendPathSegment("customformat")
                .PostJsonAsync(cf.Data)
                .ReceiveJson<JObject>();

            return (int) response["id"];
        }

        public async Task UpdateCustomFormat(int formatId, ProcessedCustomFormatData cf)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{formatId}")
                .PutJsonAsync(cf.Data);
        }

        public async Task DeleteCustomFormat(int formatId)
        {
            await BaseUrl
                .AppendPathSegment($"customformat/{formatId}")
                .DeleteAsync();
        }
    }
}
