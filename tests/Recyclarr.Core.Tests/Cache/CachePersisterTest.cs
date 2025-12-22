using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cache;

namespace Recyclarr.Core.Tests.Cache;

[CacheObjectName("test-cache")]
internal sealed record TestCacheObject : CacheObject, ITrashIdCacheObject
{
    public string? ExtraData { [UsedImplicitly] get; init; }
    public List<TrashIdMapping> Mappings { get; init; } = [];
}

// This class exists because AutoFixture does not use NSubstitute's ForPartsOf()
// See: https://github.com/AutoFixture/AutoFixture/issues/1355
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Created by AutoFixture"
)]
internal sealed class TestCachePersister(ILogger log, ICacheStoragePath storagePath)
    : CachePersister<TestCacheObject>(log, storagePath)
{
    protected override string CacheName => "Test Cache";
}

internal sealed class CachePersisterTest
{
    [Test, AutoMockData]
    public void Load_returns_default_when_file_does_not_exist(TestCachePersister sut)
    {
        var result = sut.Load();
        result.CacheObject.Should().BeEquivalentTo(new TestCacheObject());
    }

    [Test, AutoMockData]
    public void Load_returns_default_when_json_has_error(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        TestCachePersister sut
    )
    {
        const string cacheJson = """
            {\
              extra_data: Hello
            }/
            """;

        fs.AddFile("cacheFile.json", new MockFileData(cacheJson));
        storage.CalculatePath<TestCacheObject>().Returns(fs.FileInfo.New("cacheFile.json"));

        var result = sut.Load();

        result.CacheObject.Should().BeEquivalentTo(new TestCacheObject());
    }

    [Test, AutoMockData]
    public void Load_returns_cache_data(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        TestCachePersister sut
    )
    {
        const string cacheJson = """
            {
              "extra_data": "Hello"
            }
            """;

        fs.AddFile("cacheFile.json", new MockFileData(cacheJson));
        storage.CalculatePath<TestCacheObject>().Returns(fs.FileInfo.New("cacheFile.json"));

        var result = sut.Load();

        result.CacheObject.Should().BeEquivalentTo(new TestCacheObject { ExtraData = "Hello" });
    }
}
