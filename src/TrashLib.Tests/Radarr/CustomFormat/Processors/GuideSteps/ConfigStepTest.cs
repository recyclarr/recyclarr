using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.GuideSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Processors.GuideSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigStepTest
{
    [Test]
    public void Cache_names_are_used_instead_of_name_in_json_data()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1", 100),
            NewCf.Processed("name3", "id3", new TrashIdMapping("id3", "name1"))
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                Names = new List<string> {"name1"}
            }
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                    {testProcessedCfs[1]}
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }

    [Test]
    public void Custom_formats_missing_from_config_are_skipped()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", ""),
            NewCf.Processed("name2", "")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                Names = new List<string> {"name1"}
            }
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "")
                }
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }

    [Test]
    public void Custom_formats_missing_from_guide_are_added_to_not_in_guide_list()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", ""),
            NewCf.Processed("name2", "")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                Names = new List<string> {"name1", "name3"}
            }
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEquivalentTo(new List<string> {"name3"}, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "")
                }
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }

    [Test]
    public void Duplicate_config_name_and_id_are_ignored()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                Names = new List<string> {"name1"},
                TrashIds = new List<string> {"id1"}
            }
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData> {testProcessedCfs[0]}
            }
        });
    }

    [Test]
    public void Duplicate_config_names_are_ignored()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new() {Names = new List<string> {"name1", "name1"}}
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData> {testProcessedCfs[0]}
            }
        });
    }

    [Test]
    public void Find_custom_formats_by_name_and_trash_id()
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1", 100),
            NewCf.Processed("name3", "id3"),
            NewCf.Processed("name4", "id4")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                Names = new List<string> {"name1", "name3"},
                TrashIds = new List<string> {"id1", "id4"},
                QualityProfiles = new List<QualityProfileConfig>
                {
                    new() {Name = "profile1", Score = 50}
                }
            }
        };

        var processor = new ConfigStep();
        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = testProcessedCfs,
                QualityProfiles = testConfig[0].QualityProfiles
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }
}
