using Recyclarr.TrashLib.Pipelines.QualityProfile;
using Recyclarr.TrashLib.Pipelines.QualityProfile.Api;
using Recyclarr.TrashLib.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Pipelines.QualityProfile.PipelinePhases;

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
            NewQp.Processed("invalid_profile_name", (1, 100))
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
            NewQp.Processed("profile1", (1, 100), (2, 500))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
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

        result.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            UpdatedProfiles =
            {
                new UpdatedQualityProfile(new QualityProfileDto
                {
                    Name = "profile1",
                    FormatItems =
                    {
                        new ProfileFormatItemDto
                        {
                            Name = "quality1",
                            Format = 1,
                            Score = 100
                        },
                        new ProfileFormatItemDto
                        {
                            Name = "quality2",
                            Format = 2,
                            Score = 500
                        }
                    }
                })
                {
                    UpdatedScores =
                    {
                        new UpdatedFormatScore("quality1", 200, 100, FormatScoreUpdateReason.Updated),
                        new UpdatedFormatScore("quality2", 300, 500, FormatScoreUpdateReason.Updated)
                    }
                }
            }
        });
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
                FormatItems =
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
            NewQp.Processed("profile1", (1, 200), (2, 300))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
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
    public void Reset_scores(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("profile1", true, (3, 100), (4, 500))
        };

        var serviceData = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
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

        result.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            UpdatedProfiles =
            {
                new UpdatedQualityProfile(new QualityProfileDto
                {
                    Name = "profile1",
                    FormatItems =
                    {
                        new ProfileFormatItemDto
                        {
                            Name = "quality1",
                            Format = 1,
                            Score = 0
                        },
                        new ProfileFormatItemDto
                        {
                            Name = "quality2",
                            Format = 2,
                            Score = 0
                        }
                    }
                })
                {
                    UpdatedScores =
                    {
                        new UpdatedFormatScore("quality1", 200, 0, FormatScoreUpdateReason.Reset),
                        new UpdatedFormatScore("quality2", 300, 0, FormatScoreUpdateReason.Reset)
                    }
                }
            }
        });
    }
}
