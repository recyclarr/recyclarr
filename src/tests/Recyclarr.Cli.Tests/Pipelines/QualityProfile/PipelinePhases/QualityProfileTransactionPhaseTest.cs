using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Non_existent_profile_names_with_updated(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("invalid_profile_name") with
            {
                ShouldCreate = false
            },
            NewQp.Processed("profile1")
        };

        var dtos = new[]
        {
            new QualityProfileDto {Name = "profile1"}
        };

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            NonExistentProfiles = new[] {"invalid_profile_name"},
            UpdatedProfiles =
            {
                new UpdatedQualityProfile
                {
                    ProfileConfig = guideData[1],
                    ProfileDto = dtos[0],
                    UpdateReason = QualityProfileUpdateReason.Changed
                }
            }
        });
    }

    [Test, AutoMockData]
    public void New_profiles(
        QualityProfileTransactionPhase sut)
    {
        var configData = new[]
        {
            new ProcessedQualityProfileData(new QualityProfileConfig
            {
                Name = "profile1",
                Qualities = new[]
                {
                    new QualityProfileQualityConfig {Name = "quality1", Enabled = true}
                }
            })
        };

        var dtos = new[]
        {
            new QualityProfileDto {Name = "irrelevant_profile"}
        };

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto())
        {
            Schema = new QualityProfileDto
            {
                Items = new[]
                {
                    new ProfileItemDto {Quality = new ProfileItemQualityDto {Name = "quality1"}}
                }
            }
        };

        var result = sut.Execute(configData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            UpdatedProfiles =
            {
                new UpdatedQualityProfile
                {
                    ProfileConfig = configData[0],
                    ProfileDto = serviceData.Schema,
                    UpdateReason = QualityProfileUpdateReason.New,
                    UpdatedQualities = new UpdatedQualities
                    {
                        NumWantedItems = 1,
                        Items = new[]
                        {
                            new ProfileItemDto
                            {
                                Allowed = true,
                                Quality = new ProfileItemQualityDto
                                {
                                    Name = "quality1"
                                }
                            }
                        }
                    }
                }
            }
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

        var dtos = new[]
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

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

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

        var dtos = new[]
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

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

        var result = sut.Execute(guideData, serviceData);

        result.Should().BeEquivalentTo(new QualityProfileTransactionData());
    }

    [Test, AutoMockData]
    public void Unchanged_scores(
        QualityProfileTransactionPhase sut)
    {
        // Must simulate at least 1 custom format coming from configuration otherwise processing doesn't happen.
        // Profile name must match but the format IDs for each quality should not
        var guideData = new[]
        {
            NewQp.Processed("profile1", ("id1", 1, 200), ("id2", 2, 300))
        };

        var dtos = new[]
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

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

        var result = sut.Execute(guideData, serviceData);

        result.UpdatedProfiles.Should()
            .ContainSingle().Which.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("quality1", 200, 200, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("quality2", 300, 300, FormatScoreUpdateReason.NoChange)
            }, o => o.Excluding(x => x.Dto.Format));
    }

    [Test, AutoMockData]
    public void Reset_scores_with_reset_unmatched_true(
        QualityProfileTransactionPhase sut)
    {
        var guideData = new[]
        {
            NewQp.Processed("profile1", true, ("quality3", "id3", 3, 100), ("quality4", "id4", 4, 500))
        };

        var dtos = new[]
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

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

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

        var dtos = new[]
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

        var serviceData = new QualityProfileServiceData(dtos, new QualityProfileDto());

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
