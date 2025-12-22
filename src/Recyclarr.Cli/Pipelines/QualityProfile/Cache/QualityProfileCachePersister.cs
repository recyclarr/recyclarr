using Recyclarr.Cache;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Cache;

internal class QualityProfileCachePersister(ILogger log, ICacheStoragePath storagePath)
    : CachePersister<QualityProfileCacheObject>(log, storagePath)
{
    protected override string CacheName => "Quality Profile Cache";
}
