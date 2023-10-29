using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
public class QualityProfileTransactionPhaseTest
{
    [Test, AutoMockData]
    public void Non_existent_profile_names_mixed_with_valid_profiles(
        QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto {Name = "profile1"}
        };

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed("invalid_profile_name") with
                {
                    ShouldCreate = false
                },
                NewQp.Processed("profile1")
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            NonExistentProfiles = new[] {"invalid_profile_name"},
            UnchangedProfiles = new List<ProfileWithStats>
            {
                new()
                {
                    Profile = new UpdatedQualityProfile
                    {
                        ProfileConfig = context.ConfigOutput[1],
                        ProfileDto = dtos[0],
                        UpdateReason = QualityProfileUpdateReason.Changed
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void New_profiles(
        QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto {Name = "irrelevant_profile"}
        };

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                new ProcessedQualityProfileData
                {
                    Profile = new QualityProfileConfig
                    {
                        Name = "profile1",
                        Qualities = new[]
                        {
                            new QualityProfileQualityConfig {Name = "quality1", Enabled = true}
                        }
                    }
                }
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
            {
                Schema = new QualityProfileDto
                {
                    Items = new[]
                    {
                        new ProfileItemDto {Quality = new ProfileItemQualityDto {Name = "quality1"}}
                    }
                }
            }
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo(new QualityProfileTransactionData
        {
            ChangedProfiles = new List<ProfileWithStats>
            {
                new()
                {
                    QualitiesChanged = true,
                    Profile = new UpdatedQualityProfile
                    {
                        ProfileConfig = context.ConfigOutput[0],
                        ProfileDto = context.ApiFetchOutput.Schema,
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
            }
        });
    }

    [Test, AutoMockData]
    public void Updated_scores(
        QualityProfileTransactionPhase sut)
    {
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

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed("profile1", ("id1", 1, 100), ("id2", 2, 500))
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.ChangedProfiles.Should()
            .ContainSingle().Which.Profile.UpdatedScores.Should()
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

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = Array.Empty<ProcessedQualityProfileData>(),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.Should().BeEquivalentTo(new QualityProfileTransactionData());
    }

    [Test, AutoMockData]
    public void Unchanged_scores(
        QualityProfileTransactionPhase sut)
    {
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

        var context = new QualityProfilePipelineContext
        {
            // Must simulate at least 1 custom format coming from configuration otherwise processing doesn't happen.
            // Profile name must match but the format IDs for each quality should not
            ConfigOutput = new[]
            {
                NewQp.Processed("profile1", ("id1", 1, 200), ("id2", 2, 300))
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.UnchangedProfiles.Should()
            .ContainSingle().Which.Profile.UpdatedScores.Should()
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

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed("profile1", true, ("quality3", "id3", 3, 100), ("quality4", "id4", 4, 500))
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.ChangedProfiles.Should()
            .ContainSingle().Which.Profile.UpdatedScores.Should()
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
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed(new QualityProfileConfig
                    {
                        Name = "profile1",
                        ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                        {
                            Enabled = false,
                            // Throw in some exceptions here, just to test whether or not these somehow affect the
                            // result despite Enable being set to false.
                            Except = new[] {"cf1"}
                        }
                    },
                    ("cf3", "id3", 3, 100), ("cf4", "id4", 4, 500))
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.ChangedProfiles.Should()
            .ContainSingle().Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("cf1", 200, 200, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("cf2", 300, 300, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("cf3", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("cf4", 0, 500, FormatScoreUpdateReason.New)
            }, o => o.Excluding(x => x.Dto.Format));
    }

    [Test, AutoMockData]
    public void Reset_scores_with_reset_exceptions(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed(new QualityProfileConfig
                    {
                        Name = "profile1",
                        ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                        {
                            Enabled = true,
                            Except = new[] {"cf1"}
                        }
                    },
                    ("cf3", "id3", 3, 100), ("cf4", "id4", 4, 500))
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.ChangedProfiles.Should()
            .ContainSingle().Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(new[]
            {
                NewQp.UpdatedScore("cf1", 200, 200, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("cf2", 300, 0, FormatScoreUpdateReason.Reset),
                NewQp.UpdatedScore("cf3", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("cf4", 0, 500, FormatScoreUpdateReason.New)
            }, o => o.Excluding(x => x.Dto.Format));
    }

    [Test, AutoMockData]
    public void Reset_scores_with_invalid_except_list_items(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems = new[]
                {
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300
                    }
                }
            }
        };

        var context = new QualityProfilePipelineContext
        {
            ConfigOutput = new[]
            {
                NewQp.Processed(new QualityProfileConfig
                {
                    Name = "profile1",
                    ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                    {
                        Enabled = true,
                        Except = new[] {"cf50"}
                    }
                })
            },
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
        };

        sut.Execute(context);

        context.TransactionOutput.ChangedProfiles.Should()
            .ContainSingle().Which.Profile.InvalidExceptCfNames.Should()
            .BeEquivalentTo("cf50");
    }
}
