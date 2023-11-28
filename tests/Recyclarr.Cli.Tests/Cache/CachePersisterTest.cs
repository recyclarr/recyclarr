using System.Collections.ObjectModel;
using Recyclarr.Cli.Cache;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Tests.Cache;

[TestFixture]
public class CachePersisterTest
{
    private sealed class Context
    {
        public Context()
        {
            var log = Substitute.For<ILogger>();
            ServiceCache = Substitute.For<IServiceCache>();
            Persister = new CachePersister(log, ServiceCache);
        }

        public CachePersister Persister { get; }
        public IServiceCache ServiceCache { get; }
    }

    [TestCase(CustomFormatCache.LatestVersion - 1)]
    [TestCase(CustomFormatCache.LatestVersion + 1)]
    public void Throw_when_versions_mismatch(int versionToTest)
    {
        var ctx = new Context();
        var config = Substitute.For<IServiceConfiguration>();

        var testCfObj = new CustomFormatCache
        {
            Version = versionToTest,
            TrashIdMappings = new Collection<TrashIdMapping> {new("", "", 5)}
        };

        ctx.ServiceCache.Load<CustomFormatCache>(config).Returns(testCfObj);

        var act = () => ctx.Persister.Load(config);

        act.Should().Throw<CacheException>();
    }

    [Test]
    public void Accept_loaded_cache_when_versions_match()
    {
        var ctx = new Context();
        var config = Substitute.For<IServiceConfiguration>();

        var testCfObj = new CustomFormatCache
        {
            Version = CustomFormatCache.LatestVersion,
            TrashIdMappings = new Collection<TrashIdMapping> {new("", "", 5)}
        };
        ctx.ServiceCache.Load<CustomFormatCache>(config).Returns(testCfObj);
        var result = ctx.Persister.Load(config);
        result.Should().NotBeNull();
    }

    [Test]
    public void Cache_is_valid_after_successful_load()
    {
        var ctx = new Context();
        var testCfObj = new CustomFormatCache();
        var config = Substitute.For<IServiceConfiguration>();

        ctx.ServiceCache.Load<CustomFormatCache>(config).Returns(testCfObj);
        var result = ctx.Persister.Load(config);
        result.Should().BeSameAs(testCfObj);
    }

    [Test]
    public void Save_works_with_valid_cf_cache()
    {
        var ctx = new Context();
        var testCfObj = new CustomFormatCache();
        var config = Substitute.For<IServiceConfiguration>();

        ctx.ServiceCache.Load<CustomFormatCache>(config).Returns(testCfObj);

        var result = ctx.Persister.Load(config);
        ctx.Persister.Save(config, result);

        ctx.ServiceCache.Received().Save(testCfObj, config);
    }
}
