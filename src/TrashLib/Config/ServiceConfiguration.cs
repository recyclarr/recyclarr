using Flurl;
using Newtonsoft.Json;

namespace TrashLib.Config
{
    public abstract class ServiceConfiguration : IServiceConfiguration
    {
        [JsonIgnore]
        public int Id { get; set; }

        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";

        // This may need to be specialized in subclasses later.
        // For now, both Sonarr and Radarr share the same URL structure.
        public virtual string BuildUrl()
        {
            return BaseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = ApiKey});
        }
    }
}
