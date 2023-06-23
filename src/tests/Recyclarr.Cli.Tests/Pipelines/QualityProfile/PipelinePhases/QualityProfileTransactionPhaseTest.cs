using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Invalid_profile_names(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("invalid_profile_name", ("id1", 1, 100))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1"
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            InvalidProfileNames = {"invalid_profile_name"}
        });
    }

    [Test, AutoMockData]
    public void Updated_scores(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("profile1", ("id1", 1, 100), ("id2", 2, 500))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.UpdatedProfiles.Should()
            .ContainSingle().Which.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("quality1", 200, 100, FormatScoreUpdateReason.Updated),
                NewQp.UpdatedScore("quality2", 300, 500, FormatScoreUpdateReason.Updated)
            }, o => o.Excluding(x => x.Dto.Format));
    }

    [Test, AutoMockData]
    public void No_updated_profiles_when_no_custom_formats(
        QualityProfileTransactionPhase sut)
    {
        var guideData = Array.Empty<ProcessedQualityProfileData>();

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData());
    }

    [Test, AutoMockData]
    public void Skip_unchanged_scores(
        QualityProfileTransactionPhase sut)
    {
        // Must simulate at least 1 custom format coming from configuration otherwise processing doesn't happen.
        // Profile name must match but the format IDs for each quality should not
        var guideData = new[]
        {
            NewQp.Processed("profile1", ("id1", 1, 200), ("id2", 2, 300))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData());
    }

    [Test, AutoMockData]
    public void Reset_scores_with_reset_unmatched_true(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("profile1", true, ("quality3", "id3", 3, 100), ("quality4", "id4", 4, 500))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.UpdatedProfiles.Should()
            .ContainSingle().Which.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("quality1", 200, 0, FormatScoreUpdateReason.Reset),
                NewQp.UpdatedScore("quality2", 300, 0, FormatScoreUpdateReason.Reset),
                NewQp.UpdatedScore("quality3", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("quality4", 0, 500, FormatScoreUpdateReason.New)
            }, o => o.Excluding(x => x.Dto.Format));
    }

    [Test, AutoMockData]
    public void Reset_scores_with_reset_unmatched_false(QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("profile1", false, ("quality3", "id3", 3, 100), ("quality4", "id4", 4, 500))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var result = sut.Execute(guideData, serviceData);

        result.UpdatedProfiles.Should()
            .ContainSingle().Which.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("quality1", 200, 200, FormatScoreUpdateReason.Reset),
                NewQp.UpdatedScore("quality2", 300, 300, FormatScoreUpdateReason.Reset),
                NewQp.UpdatedScore("quality3", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("quality4", 0, 500, FormatScoreUpdateReason.New)
            }, o => o.Excluding(x => x.Dto.Format));
    }
}
