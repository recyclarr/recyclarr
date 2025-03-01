using Recyclarr.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Core.Tests.Cache;

// This class exists because AutoFixture does not use NSubstitute's ForPartsOf()
// See: https://github.com/AutoFixture/AutoFixture/issues/1355
public class TestCachePersister(
    ILogger log,
    ICacheStoragePath storagePath,
    IServiceConfiguration config
) : CachePersister<TestCacheObject, TestCache>(log, storagePath, config)
{
    protected override string CacheName => "Test Cache";

    protected override TestCache CreateCache(TestCacheObject cacheObject)
    {
        return new TestCache(cacheObject);
    }
}
