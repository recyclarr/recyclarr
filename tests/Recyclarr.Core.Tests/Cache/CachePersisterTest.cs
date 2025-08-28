using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Core.Tests.Cache;

[CacheObjectName("test-cache")]
internal sealed record TestCacheObject() : CacheObject(LatestVersion)
{
    public new const int LatestVersion = 1;
    public string? ExtraData { [UsedImplicitly] get; init; }
}

internal sealed class TestCache(TestCacheObject cacheObject) : BaseCache(cacheObject);

// This class exists because AutoFixture does not use NSubstitute's ForPartsOf()
// See: https://github.com/AutoFixture/AutoFixture/issues/1355
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Created by AutoFixture"
)]
internal sealed class TestCachePersister(
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

    [Test]
    [InlineAutoMockData(TestCacheObject.LatestVersion - 1)]
    [InlineAutoMockData(TestCacheObject.LatestVersion + 1)]
    public void Throw_when_versions_mismatch(
        int versionToTest,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        TestCachePersister sut
    )
    {
        var cacheJson = $$"""
            {
              "version": {{versionToTest}},
              "instance_name": "The Instance",
              "extra_data": "Hello"
            }
            """;

        fs.AddFile("cacheFile.json", new MockFileData(cacheJson));
        storage.CalculatePath<TestCacheObject>().Returns(fs.FileInfo.New("cacheFile.json"));

        var act = sut.Load;

        act.Should().Throw<CacheException>();
    }

    [Test, AutoMockData]
    public void Accept_loaded_cache_when_versions_match(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        TestCachePersister sut
    )
    {
        var cacheJson = $$"""
            {
              "version": {{TestCacheObject.LatestVersion}},
              "instance_name": "The Instance",
              "extra_data": "Hello"
            }
            """;

        fs.AddFile("cacheFile.json", new MockFileData(cacheJson));
        storage.CalculatePath<TestCacheObject>().Returns(fs.FileInfo.New("cacheFile.json"));

        var result = sut.Load();

        result
            .CacheObject.Should()
            .BeEquivalentTo(
                new TestCacheObject
                {
                    Version = TestCacheObject.LatestVersion,
                    InstanceName = "The Instance",
                    ExtraData = "Hello",
                }
            );
    }

    [Test, AutoMockData]
    public void Instance_name_is_set_when_saving(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        [Frozen] IServiceConfiguration config,
        TestCachePersister sut
    )
    {
        var cacheFilePath = fs.FileInfo.New("cacheFile.json");
        storage.CalculatePath<TestCacheObject>().Returns(cacheFilePath);
        config.InstanceName.Returns("InstanceName2");

        var cacheObject = new TestCache(new TestCacheObject { InstanceName = "InstanceName1" });

        sut.Save(cacheObject);

        fs.GetFile(cacheFilePath)
            .TextContents.Should()
            .Be(
                $$"""
                {
                  "extra_data": null,
                  "version": {{TestCacheObject.LatestVersion}},
                  "instance_name": "InstanceName2"
                }
                """
            );
    }
}
