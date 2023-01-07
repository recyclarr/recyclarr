using System.Collections.ObjectModel;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;
using Recyclarr.TrashLib.TestLibrary;
using Serilog;

namespace Recyclarr.TrashLib.Tests.CustomFormat;

[TestFixture]
[Parallelizable(ParallelScope.All)]
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
    public void Set_loaded_cache_to_null_if_versions_mismatch(int versionToTest)
    {
        var ctx = new Context();

        var testCfObj = new CustomFormatCache
        {
            Version = versionToTest,
            TrashIdMappings = new Collection<TrashIdMapping> {new("", "", 5)}
        };
        ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);
        ctx.Persister.Load();
        ctx.Persister.CfCache.Should().BeNull();
    }

    [Test]
    public void Accept_loaded_cache_when_versions_match()
    {
        var ctx = new Context();

        var testCfObj = new CustomFormatCache
        {
            Version = CustomFormatCache.LatestVersion,
            TrashIdMappings = new Collection<TrashIdMapping> {new("", "", 5)}
        };
        ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);
        ctx.Persister.Load();
        ctx.Persister.CfCache.Should().NotBeNull();
    }

    [Test]
    public void Cf_cache_is_valid_after_successful_load()
    {
        var ctx = new Context();
        var testCfObj = new CustomFormatCache();
        ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);

        ctx.Persister.Load();
        ctx.Persister.CfCache.Should().BeSameAs(testCfObj);
    }

    [Test]
    public void Cf_cache_returns_null_if_not_loaded()
    {
        var ctx = new Context();
        ctx.Persister.Load();
        ctx.Persister.CfCache.Should().BeNull();
    }

    [Test]
    public void Save_works_with_valid_cf_cache()
    {
        var ctx = new Context();
        var testCfObj = new CustomFormatCache();
        ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);

        ctx.Persister.Load();
        ctx.Persister.Save();

        ctx.ServiceCache.Received().Save(Arg.Is(testCfObj));
    }

    [Test]
    public void Saving_without_loading_does_nothing()
    {
        var ctx = new Context();
        ctx.Persister.Save();
        ctx.ServiceCache.DidNotReceive().Save(Arg.Any<object>());
    }

    [Test]
    public void Updating_overwrites_previous_cf_cache_and_updates_cf_data()
    {
        var ctx = new Context();

        // Load initial CfCache just to test that it gets replaced
        ctx.ServiceCache.Load<CustomFormatCache>().Returns(new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping> {new("trashid", "", 1)}
        });
        ctx.Persister.Load();

        // Update with new cached items
        var customFormatData = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("trashid", "name", 5)
        };
        ctx.Persister.Update(customFormatData);

        ctx.Persister.CfCache.Should().BeEquivalentTo(new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping>
            {
                new(customFormatData[0].TrashId, customFormatData[0].Name, customFormatData[0].FormatId)
            }
        });
    }

    [Test]
    public void Saving_skips_custom_formats_with_zero_id()
    {
        var ctx = new Context();

        // Update with new cached items
        var customFormatData = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("trashid1", "name", 5),
            NewCf.Processed("trashid2", "invalid")
        };
        ctx.Persister.Update(customFormatData);

        ctx.Persister.CfCache.Should().BeEquivalentTo(new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping>
            {
                new(customFormatData[0].TrashId, customFormatData[0].Name, customFormatData[0].FormatId)
            }
        });
    }

    [Test]
    public void Updating_sets_cf_cache_without_loading()
    {
        var ctx = new Context();
        ctx.Persister.Update(new List<ProcessedCustomFormatData>());
        ctx.Persister.CfCache.Should().NotBeNull();
    }
}
