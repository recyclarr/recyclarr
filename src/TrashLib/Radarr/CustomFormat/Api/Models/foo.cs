using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Radarr.CustomFormat.Api.Models
{
    public class QualityProfileData
    {
        [JsonExtensionData] private IDictionary<string, JToken> _extraJson;

        public int Id { get; set; }
        public string Name { get; set; }
        public List<FormatItemData> FormatItems { get; set; }

        public class FormatItemData
        {
            // public int Format { get; set; }
            public string Name { get; set; }
            public int Score { get; set; }
        }
    }
}
