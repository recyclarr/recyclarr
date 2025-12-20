using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Cache;

internal class QualityProfileCachePersister(ILogger log, ICacheStoragePath storagePath)
    : CachePersister<QualityProfileCacheObject, QualityProfileCache>(log, storagePath)
{
    protected override string CacheName => "Quality Profile Cache";

    protected override QualityProfileCache CreateCache(QualityProfileCacheObject cacheObject)
    {
        return new QualityProfileCache(cacheObject);
    }
}
