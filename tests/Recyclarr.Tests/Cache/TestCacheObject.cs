using Recyclarr.Cache;

namespace Recyclarr.Tests.Cache;

[CacheObjectName("test-cache")]
public record TestCacheObject() : CacheObject(LatestVersion)
{
    public new const int LatestVersion = 1;
    public string? ExtraData
    {
        [UsedImplicitly]
        get;
        init;
    }
}

public class TestCache(TestCacheObject cacheObject) : BaseCache(cacheObject);
