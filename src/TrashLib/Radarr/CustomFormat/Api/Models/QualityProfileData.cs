using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable CollectionNeverUpdated.Global

namespace TrashLib.Radarr.CustomFormat.Api.Models
{
    public class QualityProfileData
    {
        [UsedImplicitly]
        [JsonExtensionData]
        private JObject? _extraJson;

        public int Id { get; [UsedImplicitly] set; }
        public string Name { get; [UsedImplicitly] set; } = "";
        public List<FormatItemData> FormatItems { get; [UsedImplicitly] set; } = new();

        public class FormatItemData
        {
            [UsedImplicitly]
            [JsonExtensionData]
            private JObject? _extraJson;

            public int Format { get; [UsedImplicitly] set; }
            public string Name { get; [UsedImplicitly] set; } = "";
            public int Score { get; [UsedImplicitly] set; }
        }
    }
}
