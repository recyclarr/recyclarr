using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

internal class CustomFormatCachePersister(ILogger log, ICacheStoragePath storagePath)
    : CachePersister<CustomFormatCacheObject, CustomFormatCache>(log, storagePath)
{
    protected override string CacheName => "Custom Format Cache";

    protected override CustomFormatCache CreateCache(CustomFormatCacheObject cacheObject)
    {
        return new CustomFormatCache(cacheObject);
    }
}
