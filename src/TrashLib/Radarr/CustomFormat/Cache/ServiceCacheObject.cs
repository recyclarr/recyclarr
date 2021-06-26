namespace TrashLib.Radarr.CustomFormat.Cache
{
    public abstract class ServiceCacheObject
    {
        public int Id { get; set; }
        public string ServiceBaseUrl { get; set; } = default!;
    }
}
