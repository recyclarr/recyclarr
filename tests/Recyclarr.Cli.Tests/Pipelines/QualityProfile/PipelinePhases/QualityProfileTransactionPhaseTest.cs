using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

internal sealed class QualityProfileTransactionPhaseTest
{
    private static PipelinePlan CreatePlan(params PlannedQualityProfile[] profiles)
    {
        var plan = new PipelinePlan();
        foreach (var profile in profiles)
        {
            plan.AddQualityProfile(profile);
        }

        return plan;
    }

    [Test, AutoMockData]
    public async Task Non_existent_profile_names_mixed_with_valid_profiles(
        QualityProfileTransactionPhase sut
    )
    {
        var dtos = new[] { new QualityProfileDto { Name = "profile1" } };

        var invalidProfile = NewPlan.Qp("invalid_profile_name", false);
        var validProfile = NewPlan.Qp("profile1");

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(invalidProfile, validProfile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new QualityProfileTransactionData
                {
                    NonExistentProfiles = ["invalid_profile_name"],
                    UnchangedProfiles = new List<ProfileWithStats>
                    {
                        new()
                        {
                            Profile = new UpdatedQualityProfile
                            {
                                ProfileConfig = validProfile,
                                ProfileDto = dtos[0],
                                UpdateReason = QualityProfileUpdateReason.Changed,
                            },
                        },
                    },
                }
            );
    }

    [Test, AutoMockData]
    public async Task New_profiles(QualityProfileTransactionPhase sut)
    {
        var dtos = new[] { new QualityProfileDto { Name = "irrelevant_profile" } };

        var newProfile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "profile1",
                Qualities = [new QualityProfileQualityConfig { Name = "quality1", Enabled = true }],
            }
        );

        var schema = new QualityProfileDto
        {
            Items =
            [
                new ProfileItemDto { Quality = new ProfileItemQualityDto { Name = "quality1" } },
            ],
        };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(newProfile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
            {
                Schema = schema,
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new QualityProfileTransactionData
                {
                    ChangedProfiles = new List<ProfileWithStats>
                    {
                        new()
                        {
                            QualitiesChanged = true,
                            Profile = new UpdatedQualityProfile
                            {
                                ProfileConfig = newProfile,
                                ProfileDto = schema,
                                UpdateReason = QualityProfileUpdateReason.New,
                                UpdatedQualities = new UpdatedQualities
                                {
                                    NumWantedItems = 1,
                                    Items =
                                    [
                                        new ProfileItemDto
                                        {
                                            Allowed = true,
                                            Quality = new ProfileItemQualityDto
                                            {
                                                Name = "quality1",
                                            },
                                        },
                                    ],
                                },
                            },
                        },
                    },
                }
            );
    }

    [Test, AutoMockData]
    public async Task Updated_scores(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            "profile1",
            NewPlan.CfScore("id1", 1, 100),
            NewPlan.CfScore("id2", 2, 500)
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.ChangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(
                [
                    NewQp.UpdatedScore("quality1", 200, 100, FormatScoreUpdateReason.Updated),
                    NewQp.UpdatedScore("quality2", 300, 500, FormatScoreUpdateReason.Updated),
                ],
                o => o.Excluding(x => x.Dto.Format)
            );
    }

    [Test, AutoMockData]
    public async Task No_updated_profiles_when_no_custom_formats(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var context = new QualityProfilePipelineContext
        {
            Plan = new PipelinePlan(),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().BeEquivalentTo(new QualityProfileTransactionData());
    }

    [Test, AutoMockData]
    public async Task Unchanged_scores(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            "profile1",
            NewPlan.CfScore("id1", 1, 200),
            NewPlan.CfScore("id2", 2, 300)
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UnchangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(
                [
                    NewQp.UpdatedScore("quality1", 200, 200, FormatScoreUpdateReason.NoChange),
                    NewQp.UpdatedScore("quality2", 300, 300, FormatScoreUpdateReason.NoChange),
                ],
                o => o.Excluding(x => x.Dto.Format)
            );
    }

    [Test, AutoMockData]
    public async Task Reset_scores_with_reset_unmatched_true(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "quality1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "quality2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "profile1",
                ResetUnmatchedScores = new ResetUnmatchedScoresConfig { Enabled = true },
            },
            NewPlan.CfScore("quality3", "id3", 3, 100),
            NewPlan.CfScore("quality4", "id4", 4, 500)
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.ChangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(
                [
                    NewQp.UpdatedScore("quality1", 200, 0, FormatScoreUpdateReason.Reset),
                    NewQp.UpdatedScore("quality2", 300, 0, FormatScoreUpdateReason.Reset),
                    NewQp.UpdatedScore("quality3", 0, 100, FormatScoreUpdateReason.New),
                    NewQp.UpdatedScore("quality4", 0, 500, FormatScoreUpdateReason.New),
                ],
                o => o.Excluding(x => x.Dto.Format)
            );
    }

    [Test, AutoMockData]
    public async Task Reset_scores_with_reset_unmatched_false(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "profile1",
                ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                {
                    Enabled = false,
                    Except = ["cf1"],
                },
            },
            NewPlan.CfScore("cf3", "id3", 3, 100),
            NewPlan.CfScore("cf4", "id4", 4, 500)
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.ChangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(
                [
                    NewQp.UpdatedScore("cf1", 200, 200, FormatScoreUpdateReason.NoChange),
                    NewQp.UpdatedScore("cf2", 300, 300, FormatScoreUpdateReason.NoChange),
                    NewQp.UpdatedScore("cf3", 0, 100, FormatScoreUpdateReason.New),
                    NewQp.UpdatedScore("cf4", 0, 500, FormatScoreUpdateReason.New),
                ],
                o => o.Excluding(x => x.Dto.Format)
            );
    }

    [Test, AutoMockData]
    public async Task Reset_scores_with_reset_exceptions(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "profile1",
                ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                {
                    Enabled = true,
                    Except = ["cf1"],
                },
            },
            NewPlan.CfScore("cf3", "id3", 3, 100),
            NewPlan.CfScore("cf4", "id4", 4, 500)
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.ChangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.UpdatedScores.Should()
            .BeEquivalentTo(
                [
                    NewQp.UpdatedScore("cf1", 200, 200, FormatScoreUpdateReason.NoChange),
                    NewQp.UpdatedScore("cf2", 300, 0, FormatScoreUpdateReason.Reset),
                    NewQp.UpdatedScore("cf3", 0, 100, FormatScoreUpdateReason.New),
                    NewQp.UpdatedScore("cf4", 0, 500, FormatScoreUpdateReason.New),
                ],
                o => o.Excluding(x => x.Dto.Format)
            );
    }

    [Test, AutoMockData]
    public async Task Reset_scores_with_invalid_except_list_items(
        QualityProfileTransactionPhase sut
    )
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                FormatItems =
                [
                    new ProfileFormatItemDto
                    {
                        Name = "cf1",
                        Format = 1,
                        Score = 200,
                    },
                    new ProfileFormatItemDto
                    {
                        Name = "cf2",
                        Format = 2,
                        Score = 300,
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "profile1",
                ResetUnmatchedScores = new ResetUnmatchedScoresConfig
                {
                    Enabled = true,
                    Except = ["cf50"],
                },
            }
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.ChangedProfiles.Should()
            .ContainSingle()
            .Which.Profile.InvalidExceptCfNames.Should()
            .BeEquivalentTo("cf50");
    }

    [Test, AutoMockData]
    public async Task Missing_required_qualities_are_readded(QualityProfileTransactionPhase sut)
    {
        var dtos = new[]
        {
            new QualityProfileDto
            {
                Name = "profile1",
                Items =
                [
                    new ProfileItemDto
                    {
                        Quality = new ProfileItemQualityDto { Id = 1, Name = "One" },
                    },
                    new ProfileItemDto
                    {
                        Quality = new ProfileItemQualityDto { Id = 2, Name = "Two" },
                    },
                ],
            },
        };

        var profile = NewPlan.Qp(new QualityProfileConfig { Name = "profile1" });

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = new QualityProfileServiceData(dtos, new QualityProfileDto())
            {
                Schema = new QualityProfileDto
                {
                    Items =
                    [
                        new ProfileItemDto
                        {
                            Quality = new ProfileItemQualityDto { Id = 1, Name = "One" },
                        },
                        new ProfileItemDto
                        {
                            Quality = new ProfileItemQualityDto { Id = 2, Name = "Two" },
                        },
                        new ProfileItemDto
                        {
                            Quality = new ProfileItemQualityDto { Id = 3, Name = "Three" },
                        },
                    ],
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        var profiles = context.TransactionOutput.ChangedProfiles;
        profiles.Should().ContainSingle();
        profiles.First().Profile.MissingQualities.Should().BeEquivalentTo("Three");
        profiles
            .First()
            .Profile.ProfileDto.Items.Should()
            .BeEquivalentTo([
                new ProfileItemDto
                {
                    Quality = new ProfileItemQualityDto { Id = 1, Name = "One" },
                },
                new ProfileItemDto
                {
                    Quality = new ProfileItemQualityDto { Id = 2, Name = "Two" },
                },
                new ProfileItemDto
                {
                    Quality = new ProfileItemQualityDto { Id = 3, Name = "Three" },
                },
            ]);
    }
}
