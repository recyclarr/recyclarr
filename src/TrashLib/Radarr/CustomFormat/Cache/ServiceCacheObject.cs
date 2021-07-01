namespace TrashLib.Radarr.CustomFormat.Cache
{
    public abstract class ServiceCacheObject
    {
        public int Id { get; init; }
        public string ServiceBaseUrl { get; init; } = default!;
    }
}
