using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.GuideSteps;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.CustomFormat.Processors.GuideSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigStepTest
{
    [Test, AutoMockData]
    public void Custom_formats_missing_from_config_are_skipped(ConfigStep processor)
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1"),
            NewCf.Processed("name2", "id2")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                TrashIds = new List<string> {"id1"}
            }
        };

        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEmpty();
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1")
                }
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }

    [Test, AutoMockData]
    public void Custom_formats_missing_from_guide_are_added_to_not_in_guide_list(ConfigStep processor)
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1"),
            NewCf.Processed("name2", "id2")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new()
            {
                TrashIds = new List<string> {"id1", "id3"}
            }
        };

        processor.Process(testProcessedCfs, testConfig);

        processor.CustomFormatsNotInGuide.Should().BeEquivalentTo(new List<string> {"id3"}, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
        processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1")
                }
            }
        }, op => op
            .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
            .WhenTypeIs<JToken>());
    }

    [Test, AutoMockData]
    public void Duplicate_config_trash_ids_are_ignored(ConfigStep processor)
    {
        var testProcessedCfs = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("name1", "id1")
        };

        var testConfig = new CustomFormatConfig[]
        {
            new() {TrashIds = new List<string> {"id1", "id1"}}
        };

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
}
