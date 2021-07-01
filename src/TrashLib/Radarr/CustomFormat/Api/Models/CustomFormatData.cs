using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Radarr.CustomFormat.Api.Models
{
    public class CustomFormatData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<SpecificationData> Specifications { get; set; } = new();

        [JsonExtensionData, UsedImplicitly]
        public JObject? ExtraJson { get; init; }
    }

    public class SpecificationData
    {
        public string Name { get; set; }

        [JsonExtensionData, UsedImplicitly]
        public JObject? ExtraJson { get; init; }
    }
}
