using TrashLib.Radarr.CustomFormat.Cache;

namespace TrashLib.Radarr.CustomFormat.Models.Cache
{
    public class TrashIdMapping : ServiceCacheObject
    {
        public string TrashId { get; set; } = default!;
        public int CustomFormatId { get; set; }
    }
}
