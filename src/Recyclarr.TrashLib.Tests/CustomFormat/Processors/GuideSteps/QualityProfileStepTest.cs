using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Processors.GuideSteps;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.CustomFormat.Processors.GuideSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileStepTest
{
    [Test]
    public void No_score_used_if_no_score_in_config_or_guide()
    {
        var testConfigData = new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1")
                },
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "profile1"}
                }
            }
        };

        var processor = new QualityProfileStep();
        processor.Process(testConfigData);

        processor.ProfileScores.Should().BeEmpty();
        processor.CustomFormatsWithoutScore.Should().Equal(("name1", "id1", "profile1"));
    }

    [Test]
    public void Overwrite_score_from_guide_if_config_defines_score()
    {
        var testConfigData = new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("", "id1", 100)
                },
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "profile1", Score = 50}
                }
            }
        };

        var processor = new QualityProfileStep();
        processor.Process(testConfigData);

        processor.ProfileScores.Should()
            .ContainKey("profile1").WhoseValue.Should()
            .BeEquivalentTo(
                CfTestUtils.NewMapping(new FormatMappingEntry(testConfigData[0].CustomFormats.First(), 50)));

        processor.CustomFormatsWithoutScore.Should().BeEmpty();
    }

    [Test]
    public void Use_guide_score_if_no_score_in_config()
    {
        var testConfigData = new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("", "id1", 100)
                },
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "profile1"},
                    new() {Name = "profile2", Score = null}
                }
            }
        };

        var processor = new QualityProfileStep();
        processor.Process(testConfigData);

        var expectedScoreEntries =
            CfTestUtils.NewMapping(new FormatMappingEntry(testConfigData[0].CustomFormats.First(), 100));

        processor.ProfileScores.Should().BeEquivalentTo(
            new Dictionary<string, QualityProfileCustomFormatScoreMapping>
            {
                {"profile1", expectedScoreEntries},
                {"profile2", expectedScoreEntries}
            });

        processor.CustomFormatsWithoutScore.Should().BeEmpty();
    }

    [Test]
    public void Zero_score_is_not_ignored()
    {
        var testConfigData = new List<ProcessedConfigData>
        {
            new()
            {
                CustomFormats = new List<ProcessedCustomFormatData>
                {
                    NewCf.Processed("name1", "id1", 0)
                },
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "profile1"}
                }
            }
        };

        var processor = new QualityProfileStep();
        processor.Process(testConfigData);

        processor.ProfileScores.Should()
            .ContainKey("profile1").WhoseValue.Should()
            .BeEquivalentTo(CfTestUtils.NewMapping(new FormatMappingEntry(testConfigData[0].CustomFormats.First(), 0)));

        processor.CustomFormatsWithoutScore.Should().BeEmpty();
    }
}
