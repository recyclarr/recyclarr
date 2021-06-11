using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TrashLib.Radarr.CustomFormat.Guide
{
    internal class GithubCustomFormatJsonRequester : IRadarrGuideService
    {
        private readonly ISerializer _flurlSerializer;

        public GithubCustomFormatJsonRequester()
        {
            // In addition to setting the naming strategy, this also serves as a mechanism to avoid inheriting the
            // global Flurl serializer setting: MissingMemberHandling. We do not want missing members to error out
            // since we're only deserializing a subset of the github response object.
            _flurlSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            });
        }

        public async Task<IEnumerable<string>> GetCustomFormatJson()
        {
            var response = await "https://api.github.com/repos/TRaSH-/Guides/contents/docs/json/radarr"
                .WithHeader("User-Agent", "Trash Updater")
                .ConfigureRequest(settings => settings.JsonSerializer = _flurlSerializer)
                .GetJsonAsync<List<RepoContentEntry>>();

            var tasks = response
                .Where(o => o.Type == "file" && o.Name.EndsWith(".json"))
                .Select(o => DownloadJsonContents(o.DownloadUrl));

            return await Task.WhenAll(tasks);
        }

        private async Task<string> DownloadJsonContents(string jsonUrl)
        {
            return await jsonUrl.GetStringAsync();
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private record RepoContentEntry
        {
            public string Name { get; init; } = default!;
            public string Type { get; init; } = default!;
            public string DownloadUrl { get; init; } = default!;
        }
    }
}
