using System.Collections.Generic;
using TrashLib.Cache;

namespace TrashLib.Radarr.CustomFormat.Models.Cache
{
    [CacheObjectName("custom-format-cache")]
    public class CustomFormatCache
    {
        public const int LatestVersion = 1;

        public int Version { get; init; } = LatestVersion;
        public List<TrashIdMapping> TrashIdMappings { get; init; } = new();
    }

    public class TrashIdMapping
    {
        public TrashIdMapping(string trashId, int customFormatId)
        {
            TrashId = trashId;
            CustomFormatId = customFormatId;
        }

        public string TrashId { get; }
        public int CustomFormatId { get; }
    }
}
