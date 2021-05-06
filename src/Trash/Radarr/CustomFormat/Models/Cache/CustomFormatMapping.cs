using System.Collections.Generic;
using Trash.Cache;

namespace Trash.Radarr.CustomFormat.Models.Cache
{
    [CacheObjectName("custom-format-cache")]
    public class CustomFormatCache
    {
        public List<TrashIdMapping> TrashIdMappings { get; init; } = new();
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
