using System.Collections.ObjectModel;
using TrashLib.Cache;

namespace TrashLib.Radarr.CustomFormat.Models.Cache
{
    [CacheObjectName("custom-format-cache")]
    public class CustomFormatCache
    {
        public const int LatestVersion = 1;

        public int Version { get; init; } = LatestVersion;
        public Collection<TrashIdMapping> TrashIdMappings { get; init; } = new();
    }

    public class TrashIdMapping
    {
        public TrashIdMapping(string trashId, string customFormatName, int customFormatId = default)
        {
            CustomFormatName = customFormatName;
            TrashId = trashId;
            CustomFormatId = customFormatId;
        }

        public string CustomFormatName { get; set; }
        public string TrashId { get; }
        public int CustomFormatId { get; set; }
    }
}
