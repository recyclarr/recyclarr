using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

internal sealed class QualityProfileTransactionPhaseTest
{
    private static TestPlan CreatePlan(params PlannedQualityProfile[] profiles)
    {
        var plan = new TestPlan();
        foreach (var profile in profiles)
        {
            plan.AddQualityProfile(profile);
        }

        return plan;
    }

    private static TrashIdMappingStore<QualityProfileMappings> CreateCache(
        params TrashIdMapping[] mappings
    )
    {
        var cacheObject = new QualityProfileMappings();
        cacheObject.Mappings.AddRange(mappings);
        return new TrashIdMappingStore<QualityProfileMappings>(cacheObject);
    }

    [Test, AutoMockData]
    public async Task Non_existent_profile_names_mixed_with_valid_profiles(
        QualityProfileTransactionPhase sut
    )
    {
        var dtos = new[]
        {
            new QualityProfileDto { Id = 1, Name = "profile1" },
        };

        var invalidProfile = NewPlan.Qp("invalid_profile_name", false);
        var validProfile = NewPlan.Qp("profile1");

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(invalidProfile, validProfile),
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.NonExistentProfiles.Should()
            .BeEquivalentTo("invalid_profile_name");
        context
            .TransactionOutput.UnchangedProfiles.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new UpdatedQualityProfile { ProfileConfig = validProfile, ProfileDto = dtos[0] }
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
            ApiFetchOutput = NewQp.ServiceData(dtos, schema: schema),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.NewProfiles.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new UpdatedQualityProfile
                {
                    ProfileConfig = newProfile,
                    ProfileDto = schema,
                    UpdatedQualities = new UpdatedQualities
                    {
                        NumWantedItems = 1,
                        Items =
                        [
                            new ProfileItemDto
                            {
                                Allowed = true,
                                Quality = new ProfileItemQualityDto { Name = "quality1" },
                            },
                        ],
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UpdatedProfiles.Should()
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
            Plan = new TestPlan(),
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UnchangedProfiles.Should()
            .ContainSingle()
            .Which.UpdatedScores.Should()
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UpdatedProfiles.Should()
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UpdatedProfiles.Should()
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UpdatedProfiles.Should()
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(dtos),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.UpdatedProfiles.Should()
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
                Id = 1,
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
            ApiFetchOutput = NewQp.ServiceData(
                dtos,
                schema: new QualityProfileDto
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
                }
            ),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        var profiles = context.TransactionOutput.UpdatedProfiles;
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

    [Test, AutoMockData]
    public async Task Guide_profile_with_cache_hit_updates_existing(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Guide Profile");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "Guide Profile", TrashId = "trash-id-1" },
            resource
        );

        var serviceDto = new QualityProfileDto { Id = 42, Name = "Guide Profile" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([serviceDto]),
            State = CreateCache(new TrashIdMapping("trash-id-1", "Guide Profile", 42)),
        };

        await sut.Execute(context, CancellationToken.None);

        // Profile matched by cached ID should be processed as update (not new)
        context.TransactionOutput.NewProfiles.Should().BeEmpty();
        var allUpdated = context
            .TransactionOutput.UpdatedProfiles.Select(p => p.Profile)
            .Concat(context.TransactionOutput.UnchangedProfiles)
            .ToList();
        allUpdated.Should().ContainSingle().Which.ProfileDto.Id.Should().Be(42);
    }

    [Test, AutoMockData]
    public async Task Guide_profile_with_cache_hit_renames_when_name_differs(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "New Guide Name");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "New Guide Name", TrashId = "trash-id-1" },
            resource
        );

        var serviceDto = new QualityProfileDto { Id = 42, Name = "Old Service Name" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([serviceDto]),
            State = CreateCache(new TrashIdMapping("trash-id-1", "Old Service Name", 42)),
        };

        await sut.Execute(context, CancellationToken.None);

        // Profile matched by ID should be updated (will trigger rename)
        var allUpdated = context
            .TransactionOutput.UpdatedProfiles.Select(p => p.Profile)
            .Concat(context.TransactionOutput.UnchangedProfiles)
            .ToList();
        allUpdated.Should().ContainSingle().Which.ProfileDto.Id.Should().Be(42);
    }

    [Test, AutoMockData]
    public async Task Guide_profile_with_stale_cache_falls_back_to_name_collision(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Guide Profile");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "Guide Profile", TrashId = "trash-id-1" },
            resource
        );

        // Cached ID 999 doesn't exist in service, but a profile with matching name does
        var serviceDtoWithName = new QualityProfileDto { Id = 50, Name = "Guide Profile" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoWithName]),
            State = CreateCache(new TrashIdMapping("trash-id-1", "Guide Profile", 999)),
        };

        await sut.Execute(context, CancellationToken.None);

        // Stale state + name match for guide profile = conflict (needs state repair --adopt)
        context.TransactionOutput.ConflictingProfiles.Should().ContainSingle();
        context.TransactionOutput.ConflictingProfiles[0].ConflictingId.Should().Be(50);
    }

    [Test, AutoMockData]
    public async Task Guide_profile_no_cache_name_exists_creates_conflict(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Existing Profile");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "Existing Profile", TrashId = "trash-id-1" },
            resource
        );

        var existingDto = new QualityProfileDto { Id = 100, Name = "Existing Profile" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([existingDto]),
            State = CreateCache(), // No cache entries
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.ConflictingProfiles.Should().ContainSingle();
        context.TransactionOutput.ConflictingProfiles[0].PlannedProfile.Should().Be(profile);
        context.TransactionOutput.ConflictingProfiles[0].ConflictingId.Should().Be(100);
    }

    [Test, AutoMockData]
    public async Task Guide_profile_no_cache_no_match_creates_new(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Brand New Profile");
        var profile = NewPlan.Qp(
            new QualityProfileConfig
            {
                Name = "Brand New Profile",
                TrashId = "trash-id-1",
                Qualities = [new QualityProfileQualityConfig { Name = "quality1", Enabled = true }],
            },
            resource
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
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([], schema: schema),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.NewProfiles.Should().ContainSingle();
    }

    [Test, AutoMockData]
    public async Task Guide_profile_multiple_name_matches_creates_ambiguous(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Duplicate Name");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "Duplicate Name", TrashId = "trash-id-1" },
            resource
        );

        // Two profiles with the same name (case-insensitive)
        var dto1 = new QualityProfileDto { Id = 1, Name = "Duplicate Name" };
        var dto2 = new QualityProfileDto { Id = 2, Name = "duplicate name" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([dto1, dto2]),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.AmbiguousProfiles.Should().ContainSingle();
        context.TransactionOutput.AmbiguousProfiles[0].PlannedProfile.Should().Be(profile);
        context.TransactionOutput.AmbiguousProfiles[0].ServiceMatches.Should().HaveCount(2);
    }

    [Test, AutoMockData]
    public async Task Guide_profile_no_match_with_should_create_false_adds_to_nonexistent(
        QualityProfileTransactionPhase sut
    )
    {
        var resource = NewPlan.QpResource("trash-id-1", "Missing Profile");
        var profile = NewPlan.Qp(
            new QualityProfileConfig { Name = "Missing Profile", TrashId = "trash-id-1" },
            false, // ShouldCreate = false
            resource
        );

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profile),
            ApiFetchOutput = NewQp.ServiceData([]),
            State = CreateCache(),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.NonExistentProfiles.Should().ContainSingle();
        context.TransactionOutput.NonExistentProfiles.Should().Contain("Missing Profile");
    }

    // Helper to create a guide-backed PlannedQualityProfile with explicit Name control.
    // NewPlan.Qp uses resource.Name which doesn't work when testing multiple profiles
    // sharing one resource with different names.
    private static PlannedQualityProfile GuideQp(
        string name,
        string trashId,
        QualityProfileConfig? config = null
    )
    {
        var resource = NewPlan.QpResource(trashId, "Guide Default Name");
        return new PlannedQualityProfile
        {
            Name = name,
            Config = config ?? new QualityProfileConfig { Name = name, TrashId = trashId },
            Resource = resource,
            ShouldCreate = true,
        };
    }

    [Test, AutoMockData]
    public async Task Two_profiles_same_trash_id_both_resolve_via_exact_match(
        QualityProfileTransactionPhase sut
    )
    {
        var profileA = GuideQp("A", "trash-id-1");
        var profileB = GuideQp("B", "trash-id-1");

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var serviceDtoB = new QualityProfileDto { Id = 43, Name = "B" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileA, profileB),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA, serviceDtoB]),
            State = CreateCache(
                new TrashIdMapping("trash-id-1", "A", 42),
                new TrashIdMapping("trash-id-1", "B", 43)
            ),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.NewProfiles.Should().BeEmpty();
        context.TransactionOutput.ConflictingProfiles.Should().BeEmpty();
        var allExisting = context
            .TransactionOutput.UpdatedProfiles.Select(p => p.Profile)
            .Concat(context.TransactionOutput.UnchangedProfiles)
            .ToList();
        allExisting.Select(p => p.ProfileDto.Id).Should().BeEquivalentTo([42, 43]);
    }

    [Test, AutoMockData]
    public async Task Same_trash_id_rename_one_of_two(QualityProfileTransactionPhase sut)
    {
        var profileA = GuideQp("A", "trash-id-1");
        var profileB2 = GuideQp("B2", "trash-id-1");

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var serviceDtoB = new QualityProfileDto { Id = 43, Name = "B" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileA, profileB2),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA, serviceDtoB]),
            State = CreateCache(
                new TrashIdMapping("trash-id-1", "A", 42),
                new TrashIdMapping("trash-id-1", "B", 43)
            ),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.NewProfiles.Should().BeEmpty();
        context.TransactionOutput.ConflictingProfiles.Should().BeEmpty();
        var allExisting = context
            .TransactionOutput.UpdatedProfiles.Select(p => p.Profile)
            .Concat(context.TransactionOutput.UnchangedProfiles)
            .ToList();
        allExisting.Select(p => p.ProfileDto.Id).Should().BeEquivalentTo([42, 43]);
    }

    [Test, AutoMockData]
    public async Task Same_trash_id_add_second_instance(QualityProfileTransactionPhase sut)
    {
        var profileA = GuideQp("A", "trash-id-1");
        var profileClone = GuideQp(
            "Clone",
            "trash-id-1",
            new QualityProfileConfig
            {
                Name = "Clone",
                TrashId = "trash-id-1",
                Qualities = [new QualityProfileQualityConfig { Name = "quality1", Enabled = true }],
            }
        );

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var schema = new QualityProfileDto
        {
            Items =
            [
                new ProfileItemDto { Quality = new ProfileItemQualityDto { Name = "quality1" } },
            ],
        };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileA, profileClone),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA], schema: schema),
            State = CreateCache(new TrashIdMapping("trash-id-1", "A", 42)),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.NewProfiles.Should().ContainSingle();
        context.TransactionOutput.NewProfiles[0].ProfileConfig.Name.Should().Be("Clone");
        var allExisting = context
            .TransactionOutput.UpdatedProfiles.Select(p => p.Profile)
            .Concat(context.TransactionOutput.UnchangedProfiles)
            .ToList();
        allExisting.Should().ContainSingle().Which.ProfileDto.Id.Should().Be(42);
    }

    [Test, AutoMockData]
    public async Task Same_trash_id_rename_two_of_two_creates_new(
        QualityProfileTransactionPhase sut
    )
    {
        var profileA2 = GuideQp(
            "A2",
            "trash-id-1",
            new QualityProfileConfig
            {
                Name = "A2",
                TrashId = "trash-id-1",
                Qualities = [new QualityProfileQualityConfig { Name = "quality1", Enabled = true }],
            }
        );
        var profileB2 = GuideQp(
            "B2",
            "trash-id-1",
            new QualityProfileConfig
            {
                Name = "B2",
                TrashId = "trash-id-1",
                Qualities = [new QualityProfileQualityConfig { Name = "quality1", Enabled = true }],
            }
        );

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var serviceDtoB = new QualityProfileDto { Id = 43, Name = "B" };
        var schema = new QualityProfileDto
        {
            Items =
            [
                new ProfileItemDto { Quality = new ProfileItemQualityDto { Name = "quality1" } },
            ],
        };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileA2, profileB2),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA, serviceDtoB], schema: schema),
            State = CreateCache(
                new TrashIdMapping("trash-id-1", "A", 42),
                new TrashIdMapping("trash-id-1", "B", 43)
            ),
        };

        await sut.Execute(context, CancellationToken.None);

        // Neither matches in Pass 1; 2 unclaimed in Pass 2 = ambiguous; both create new
        context.TransactionOutput.NewProfiles.Should().HaveCount(2);
        context
            .TransactionOutput.NewProfiles.Select(p => p.ProfileConfig.Name)
            .Should()
            .BeEquivalentTo("A2", "B2");
    }

    [Test, AutoMockData]
    public async Task Rename_to_name_taken_by_service_profile_creates_conflict(
        QualityProfileTransactionPhase sut
    )
    {
        var profileC = GuideQp("C", "trash-id-1");

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var serviceDtoC = new QualityProfileDto { Id = 99, Name = "C" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileC),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA, serviceDtoC]),
            State = CreateCache(new TrashIdMapping("trash-id-1", "A", 42)),
        };

        await sut.Execute(context, CancellationToken.None);

        // Rename detected in Pass 2, but ProcessCachedProfile sees "C" is taken by id 99
        context.TransactionOutput.ConflictingProfiles.Should().ContainSingle();
        context.TransactionOutput.ConflictingProfiles[0].ConflictingId.Should().Be(99);
    }

    [Test, AutoMockData]
    public async Task Rename_to_name_with_multiple_service_matches_creates_ambiguous(
        QualityProfileTransactionPhase sut
    )
    {
        var profileC = GuideQp("C", "trash-id-1");

        var serviceDtoA = new QualityProfileDto { Id = 42, Name = "A" };
        var serviceDtoC1 = new QualityProfileDto { Id = 98, Name = "C" };
        var serviceDtoC2 = new QualityProfileDto { Id = 99, Name = "c" };

        var context = new QualityProfilePipelineContext
        {
            Plan = CreatePlan(profileC),
            ApiFetchOutput = NewQp.ServiceData([serviceDtoA, serviceDtoC1, serviceDtoC2]),
            State = CreateCache(new TrashIdMapping("trash-id-1", "A", 42)),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.AmbiguousProfiles.Should().ContainSingle();
        context.TransactionOutput.AmbiguousProfiles[0].ServiceMatches.Should().HaveCount(2);
    }
}
