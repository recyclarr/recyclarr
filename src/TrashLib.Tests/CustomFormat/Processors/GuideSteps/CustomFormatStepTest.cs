using System.Collections.ObjectModel;
using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TestLibrary.FluentAssertions;
using TrashLib.Config.Services;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;
using TrashLib.Services.CustomFormat.Processors.GuideSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.CustomFormat.Processors.GuideSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatStepTest
{
    private class Context
    {
        public List<CustomFormatData> TestGuideData { get; } = new()
        {
            NewCf.Data("name1", "id1"),
            NewCf.Data("name2", "id2"),
            NewCf.Data("name3", "id3")
        };
    }

    [Test, AutoMockData]
    public void Cfs_not_in_config_are_skipped(CustomFormatStep processor)
    {
        var ctx = new Context();
        var testConfig = new List<CustomFormatConfig>
        {
            new() {TrashIds = new List<string> {"id1", "id3"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name1", "id1"),
                NewCf.Processed("name3", "id3")
            });
    }

    [Test, AutoMockData]
    public void Config_cfs_in_different_sections_are_processed(CustomFormatStep processor)
    {
        var ctx = new Context();
        var testConfig = new List<CustomFormatConfig>
        {
            new() {TrashIds = new List<string> {"id1", "id3"}},
            new() {TrashIds = new List<string> {"id2"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should().BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name1", "id1"),
                NewCf.Processed("name2", "id2"),
                NewCf.Processed("name3", "id3")
            },
            op => op.Using(new JsonEquivalencyStep()));
    }

    [Test, AutoMockData]
    public void Custom_format_is_deleted_if_in_config_and_cache_but_not_in_guide(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            NewCf.Data("name1", "id1")
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new() {TrashIds = new List<string> {"id1"}}
        };

        var testCache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping> {new("id1000")}
        };

        processor.Process(guideData, testConfig, testCache);

        processor.DeletedCustomFormatsInCache.Should()
            .BeEquivalentTo(new[] {new TrashIdMapping("id1000")});
        processor.ProcessedCustomFormats.Should().BeEquivalentTo(new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1")
        });
    }

    [Test, AutoMockData]
    public void Custom_format_is_deleted_if_not_in_config_but_in_cache_and_in_guide(CustomFormatStep processor)
    {
        var cache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping> {new("id1", 9)}
        };

        var guideCfs = new List<CustomFormatData>
        {
            NewCf.Data("3D", "id1")
        };

        processor.Process(guideCfs, Array.Empty<CustomFormatConfig>(), cache);

        processor.DeletedCustomFormatsInCache.Should().BeEquivalentTo(new[] {cache.TrashIdMappings[0]});
        processor.ProcessedCustomFormats.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Match_custom_format_using_trash_id(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            NewCf.Data("name1", "id1"),
            NewCf.Data("name2", "id2")
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new() {TrashIds = new List<string> {"id2"}}
        };

        processor.Process(guideData, testConfig, null);

        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name2", "id2")
            });
    }

    [Test, AutoMockData]
    public void Non_existent_cfs_in_config_are_skipped(CustomFormatStep processor)
    {
        var ctx = new Context();
        var testConfig = new List<CustomFormatConfig>
        {
            new() {TrashIds = new List<string> {"doesnt_exist"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Score_from_json_takes_precedence_over_score_from_guide(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            NewCf.Data("name1", "id1", 100)
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new()
            {
                TrashIds = new List<string> {"id1"},
                QualityProfiles = new List<QualityProfileConfig>
                {
                    new() {Name = "profile", Score = 200}
                }
            }
        };

        processor.Process(guideData, testConfig, null);

        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1", 100)
                },
                op => op.Using(new JsonEquivalencyStep()));
    }
}
