using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Trash.Config;
using Trash.Radarr.CustomFormat.Models;

namespace Trash.Radarr.CustomFormat.Api
{
    internal class CustomFormatService : ICustomFormatService
    {
        private readonly IServiceConfiguration _serviceConfig;

        public CustomFormatService(IServiceConfiguration serviceConfig)
        {
            _serviceConfig = serviceConfig;
        }

        public async Task<List<JObject>> GetCustomFormats()
        {
            return await BaseUrl()
                .AppendPathSegment("customformat")
                .GetJsonAsync<List<JObject>>();
        }

        public async Task CreateCustomFormat(ProcessedCustomFormatData cf)
        {
            var response = await BaseUrl()
                .AppendPathSegment("customformat")
                .PostJsonAsync(cf.Json)
                .ReceiveJson<JObject>();

            cf.SetCache((int) response["id"]);
        }

        public async Task UpdateCustomFormat(ProcessedCustomFormatData cf)
        {
            // Set the cache first, since it's needed to perform the update. This case will apply to CFs we update that
            // exist in Radarr but not the cache (e.g. moving to a new machine, same-named CF was created manually)
            if (cf.CacheEntry == null)
            {
                cf.SetCache((int) cf.Json["id"]);
            }

            await BaseUrl()
                .AppendPathSegment($"customformat/{cf.GetCustomFormatId()}")
                .PutJsonAsync(cf.Json)
                .ReceiveJson<JObject>();
        }

        public async Task DeleteCustomFormat(int customFormatId)
        {
            await BaseUrl()
                .AppendPathSegment($"customformat/{customFormatId}")
                .DeleteAsync();
        }

        private string BaseUrl()
        {
            return _serviceConfig.BuildUrl();
        }
    }
}
