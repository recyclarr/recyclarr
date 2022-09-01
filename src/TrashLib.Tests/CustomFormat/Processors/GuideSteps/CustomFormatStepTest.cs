using System.Collections.ObjectModel;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TestLibrary.FluentAssertions;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;
using TrashLib.Services.CustomFormat.Processors.GuideSteps;
using TrashLib.Services.Radarr.Config;
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

    [TestCase("name1", 0)]
    [TestCase("naME1", 0)]
    [TestCase("DifferentName", 1)]
    public void Match_cf_in_guide_with_different_name_with_cache_using_same_name_in_config(string variableCfName,
        int outdatedCount)
    {
        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name1"}}
        };

        var testCache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping>
            {
                new("id1", "name1")
            }
        };

        var testGuideData = new List<CustomFormatData>
        {
            NewCf.Data(variableCfName, "id1")
        };

        var processor = new CustomFormatStep();
        processor.Process(testGuideData, testConfig, testCache);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().HaveCount(outdatedCount);
        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should().BeEquivalentTo(new List<ProcessedCustomFormatData>
        {
            NewCf.Processed(variableCfName, "id1", testCache.TrashIdMappings[0])
        });
    }

    [Test, AutoMockData]
    public void Cache_entry_is_not_set_when_id_is_different(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            NewCf.Data("name1", "id1")
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name1"}}
        };

        var testCache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping>
            {
                new("id1000", "name1")
            }
        };

        processor.Process(guideData, testConfig, testCache);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Count.Should().Be(1);
        processor.ProcessedCustomFormats.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name1", "id1")
            });
    }

    [Test, AutoMockData]
    public void Cfs_not_in_config_are_skipped(CustomFormatStep processor)
    {
        var ctx = new Context();
        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name1", "name3"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
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
            new() {Names = new List<string> {"name1", "name3"}},
            new() {Names = new List<string> {"name2"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
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
            new() {Names = new List<string> {"name1"}}
        };

        var testCache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping> {new("id1000", "name1")}
        };

        processor.Process(guideData, testConfig, testCache);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should()
            .BeEquivalentTo(new[] {new TrashIdMapping("id1000", "name1")});
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
            TrashIdMappings = new Collection<TrashIdMapping> {new("id1", "3D", 9)}
        };

        var guideCfs = new List<CustomFormatData>
        {
            new("3D", "id1", null, new JObject())
        };

        processor.Process(guideCfs, Array.Empty<CustomFormatConfig>(), cache);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should().BeEquivalentTo(new[] {cache.TrashIdMappings[0]});
        processor.ProcessedCustomFormats.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Custom_format_name_in_cache_is_updated_if_renamed_in_guide_and_config(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            new("name2", "id1", null, new JObject())
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name2"}}
        };

        var testCache = new CustomFormatCache
        {
            TrashIdMappings = new Collection<TrashIdMapping> {new("id1", "name1")}
        };

        processor.Process(guideData, testConfig, testCache);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should()
            .ContainSingle().Which.CacheEntry.Should()
            .BeEquivalentTo(new TrashIdMapping("id1", "name2"));
    }

    [Test, AutoMockData]
    public void Duplicates_are_recorded_and_removed_from_processed_custom_formats_list(CustomFormatStep processor)
    {
        var guideData = new List<CustomFormatData>
        {
            NewCf.Data("name1", "id1"),
            NewCf.Data("name1", "id2")
        };

        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name1"}}
        };

        processor.Process(guideData, testConfig, null);

        //Dictionary<string, List<ProcessedCustomFormatData>>
        processor.DuplicatedCustomFormats.Should()
            .ContainKey("name1").WhoseValue.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name1", "id1"),
                NewCf.Processed("name1", "id2")
            });
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Match_cf_names_regardless_of_case_in_config(CustomFormatStep processor)
    {
        var ctx = new Context();
        var testConfig = new List<CustomFormatConfig>
        {
            new() {Names = new List<string> {"name1", "NAME1"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should().BeEquivalentTo(new List<ProcessedCustomFormatData>
            {
                NewCf.Processed("name1", "id1")
            },
            op => op.Using(new JsonEquivalencyStep()));
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

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
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
            new() {Names = new List<string> {"doesnt_exist"}}
        };

        processor.Process(ctx.TestGuideData, testConfig, new CustomFormatCache());

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
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
                Names = new List<string> {"name1"},
                QualityProfiles = new List<QualityProfileConfig>
                {
                    new() {Name = "profile", Score = 200}
                }
            }
        };

        processor.Process(guideData, testConfig, null);

        processor.DuplicatedCustomFormats.Should().BeEmpty();
        processor.CustomFormatsWithOutdatedNames.Should().BeEmpty();
        processor.DeletedCustomFormatsInCache.Should().BeEmpty();
        processor.ProcessedCustomFormats.Should()
            .BeEquivalentTo(new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1", 100)
                },
                op => op.Using(new JsonEquivalencyStep()));
    }
}
