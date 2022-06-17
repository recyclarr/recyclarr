using System.Diagnostics.CodeAnalysis;
using Common;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.FluentAssertions;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Processors;
using TrashLib.Radarr.CustomFormat.Processors.GuideSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Processors;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class GuideProcessorTest
{
    private class TestGuideProcessorSteps : IGuideProcessorSteps
    {
        public ICustomFormatStep CustomFormat { get; } = new CustomFormatStep();
        public IConfigStep Config { get; } = new ConfigStep();
        public IQualityProfileStep QualityProfile { get; } = new QualityProfileStep();
    }

    private class Context
    {
        public Context()
        {
            Data = new ResourceDataReader(typeof(GuideProcessorTest), "Data");
        }

        public ResourceDataReader Data { get; }

        public CustomFormatData ReadCustomFormat(string textFile) =>
            LocalRepoCustomFormatJsonParser.ParseCustomFormatData(ReadText(textFile));

        public string ReadText(string textFile) => Data.ReadData(textFile);
        public JObject ReadJson(string jsonFile) => JObject.Parse(ReadText(jsonFile));
    }

    [Test]
    [SuppressMessage("Maintainability", "CA1506", Justification = "Designed to be a high-level integration test")]
    public async Task Guide_processor_behaves_as_expected_with_normal_guide_data()
    {
        var ctx = new Context();
        var guideService = Substitute.For<IRadarrGuideService>();
        var guideProcessor = new GuideProcessor(guideService, () => new TestGuideProcessorSteps());

        // simulate guide data
        guideService.GetCustomFormatData().Returns(new[]
        {
            ctx.ReadCustomFormat("ImportableCustomFormat1.json"),
            ctx.ReadCustomFormat("ImportableCustomFormat2.json"),
            ctx.ReadCustomFormat("NoScore.json"),
            ctx.ReadCustomFormat("WontBeInConfig.json")
        });

        // Simulate user config in YAML
        var config = new List<CustomFormatConfig>
        {
            new()
            {
                Names = new List<string> {"Surround SOUND", "DTS-HD/DTS:X", "no score", "not in guide 1"},
                QualityProfiles = new List<QualityProfileConfig>
                {
                    new() {Name = "profile1"},
                    new() {Name = "profile2", Score = -1234}
                }
            },
            new()
            {
                Names = new List<string> {"no score", "not in guide 2"},
                QualityProfiles = new List<QualityProfileConfig>
                {
                    new() {Name = "profile3"},
                    new() {Name = "profile4", Score = 5678}
                }
            }
        };

        await guideProcessor.BuildGuideDataAsync(config, null);

        var expectedProcessedCustomFormatData = new List<ProcessedCustomFormatData>
        {
            NewCf.Processed("Surround Sound", "43bb5f09c79641e7a22e48d440bd8868", 500,
                ctx.ReadJson("ImportableCustomFormat1_Processed.json")),
            NewCf.Processed("DTS-HD/DTS:X", "4eb3c272d48db8ab43c2c85283b69744", 480,
                ctx.ReadJson("ImportableCustomFormat2_Processed.json")),
            NewCf.Processed("No Score", "abc")
        };

        guideProcessor.ProcessedCustomFormats.Should().BeEquivalentTo(expectedProcessedCustomFormatData);

        guideProcessor.ConfigData.Should()
            .BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = expectedProcessedCustomFormatData,
                    QualityProfiles = config[0].QualityProfiles
                },
                new()
                {
                    CustomFormats = expectedProcessedCustomFormatData.GetRange(2, 1),
                    QualityProfiles = config[1].QualityProfiles
                }
            }, op => op.Using(new JsonEquivalencyStep()));

        guideProcessor.CustomFormatsWithoutScore.Should()
            .Equal(new List<(string name, string trashId, string profileName)>
            {
                ("No Score", "abc", "profile1"),
                ("No Score", "abc", "profile3")
            });

        guideProcessor.CustomFormatsNotInGuide.Should().Equal(new List<string>
        {
            "not in guide 1", "not in guide 2"
        });

        guideProcessor.ProfileScores.Should()
            .BeEquivalentTo(new Dictionary<string, QualityProfileCustomFormatScoreMapping>
            {
                {
                    "profile1", CfTestUtils.NewMapping(
                        new FormatMappingEntry(expectedProcessedCustomFormatData[0], 500),
                        new FormatMappingEntry(expectedProcessedCustomFormatData[1], 480))
                },
                {
                    "profile2", CfTestUtils.NewMapping(
                        new FormatMappingEntry(expectedProcessedCustomFormatData[0], -1234),
                        new FormatMappingEntry(expectedProcessedCustomFormatData[1], -1234),
                        new FormatMappingEntry(expectedProcessedCustomFormatData[2], -1234))
                },
                {
                    "profile4", CfTestUtils.NewMapping(
                        new FormatMappingEntry(expectedProcessedCustomFormatData[2], 5678))
                }
            }, op => op
                .Using(new JsonEquivalencyStep())
                .ComparingByMembers<FormatMappingEntry>());
    }
}
