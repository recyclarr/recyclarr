using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class PlanBuilderCfGroupTest : PlanBuilderTestBase
{
    [Test]
    public void Build_with_cf_group_resolves_cfs_to_guide_backed_profiles()
    {
        // Setup CFs that the group references
        SetupCustomFormatWithScores("TrueHD", "truehd-cf", ("default", 100));
        SetupCustomFormatWithScores("DTS-X", "dtsx-cf", ("default", 200));

        // Setup guide QP that the config references
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        // Setup CF group with Default=true CFs (included by default per opt-in semantics)
        SetupCfGroupGuideData(
            "audio-group",
            "Audio Formats",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "truehd-cf",
                    Name = "TrueHD",
                    Default = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "dtsx-cf",
                    Name = "DTS-X",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" }
        );

        // Config with CF group and guide-backed QP (implicit assignment)
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "audio-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // CFs from group should be in plan
        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId)
            .Should()
            .BeEquivalentTo("truehd-cf", "dtsx-cf");

        // CFs should be assigned to the guide-backed profile
        var profile = plan.QualityProfiles.Single();
        profile.CfScores.Should().HaveCount(2);
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_explicit_assign_scores_to()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("profile-a", "Profile A", ("HDTV-1080p", true, null));
        SetupQualityProfileGuideData("profile-b", "Profile B", ("HDTV-1080p", true, null));

        // CF is Default=true so it's included by default
        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string>
            {
                ["Profile A"] = "profile-a",
                ["Profile B"] = "profile-b",
            }
        );

        // Config with explicit assign_scores_to targeting only profile-a
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { TrashId = "profile-a" },
                        ],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "profile-a" },
                new QualityProfileConfig { TrashId = "profile-b" },
            ],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // CF should only be assigned to profile-a, not profile-b
        var profileA = plan.QualityProfiles.Single(p => p.Name == "Profile A");
        var profileB = plan.QualityProfiles.Single(p => p.Name == "Profile B");

        profileA.CfScores.Should().ContainSingle();
        profileB.CfScores.Should().BeEmpty();
    }

    [Test]
    public void Build_with_cf_group_select_adds_non_default_cf_alongside_defaults()
    {
        // Two default CFs + one non-default CF
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupCustomFormatWithScores("CF Three", "cf3", ("default", 300));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "cf2",
                    Name = "CF Two",
                    Default = true,
                },
                new CfGroupCustomFormat { TrashId = "cf3", Name = "CF Three" },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Select adds non-default cf3 alongside defaults cf1 and cf2
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group", Select = ["cf3"] }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // All defaults + selected non-default
        plan.CustomFormats.Select(x => x.Resource.TrashId)
            .Should()
            .BeEquivalentTo("cf1", "cf2", "cf3");
    }

    [Test]
    public void Build_with_cf_group_exclude_removes_default_cf()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "cf2",
                    Name = "CF Two",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group", Exclude = ["cf2"] }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().ContainSingle().Which.Resource.TrashId.Should().Be("cf1");
    }

    [Test]
    public void Build_with_cf_group_guide_profile_inclusion_filters_profiles()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("allowed-qp", "Allowed Profile", ("HDTV-1080p", true, null));
        SetupQualityProfileGuideData("excluded-qp", "Excluded Profile", ("HDTV-1080p", true, null));

        // Group's include list only contains allowed-qp, NOT excluded-qp
        // CF is Default=true so it's included by default
        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Allowed Profile"] = "allowed-qp" }
        );

        // Config has both profiles, but group only includes one
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group" }],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "allowed-qp" },
                new QualityProfileConfig { TrashId = "excluded-qp" },
            ],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // CF should only be assigned to allowed-qp (not in include list means no assignment)
        var allowedProfile = plan.QualityProfiles.Single(p => p.Name == "Allowed Profile");
        var excludedProfile = plan.QualityProfiles.Single(p => p.Name == "Excluded Profile");

        allowedProfile.CfScores.Should().ContainSingle();
        excludedProfile.CfScores.Should().BeEmpty();
    }

    [Test]
    public void Build_with_invalid_cf_group_trash_id_reports_error()
    {
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        // Config references non-existent group
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "nonexistent-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // No CFs in plan (group was skipped due to error)
        plan.CustomFormats.Should().BeEmpty();
        publisher.Received().AddError(Arg.Is<string>(s => s.Contains("nonexistent-group")));
    }

    [Test]
    public void Build_with_cf_group_no_profiles_skips_group()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "cf1", Name = "CF One" }]
        );

        // Config with CF group but no guide-backed profiles (user-defined only)
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { Name = "User Profile" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // No CFs from group (no guide-backed profiles to assign to)
        plan.CustomFormats.Should().BeEmpty();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_no_cfs_selected_skips_group()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        // CF is optional (not Default, not Required) - only included if explicitly selected
        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "cf1", Name = "CF One" }],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Config does NOT select any CFs (no select list = use defaults, but there are none)
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // No CFs in plan (optional CF not selected)
        plan.CustomFormats.Should().BeEmpty();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_selecting_required_cf_still_includes_it()
    {
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        // Setup group with a required CF
        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Config explicitly selects the required CF (redundant - required CFs are always included)
        // This is allowed but logs a warning (not a sync-blocking diagnostic)
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Select = ["required-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Required CF is still included (cannot be excluded)
        plan.CustomFormats.Should()
            .ContainSingle()
            .Which.Resource.TrashId.Should()
            .Be("required-cf");
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_required_cf_included_when_select_specifies_others()
    {
        // Setup: Group with Required CF + Default CF + Optional CF
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupCustomFormatWithScores("Default CF", "default-cf", ("default", 200));
        SetupCustomFormatWithScores("Optional CF", "optional-cf", ("default", 300));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "default-cf",
                    Name = "Default CF",
                    Default = true,
                },
                new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Select adds the optional CF; defaults and required are still included
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Select = ["optional-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Composition: required + defaults + selected
        plan.CustomFormats.Select(x => x.Resource.TrashId)
            .Should()
            .BeEquivalentTo("required-cf", "default-cf", "optional-cf");
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_baseline_includes_required_and_default()
    {
        // Setup: Group with Required CF + Default CF + Optional CF
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupCustomFormatWithScores("Default CF", "default-cf", ("default", 200));
        SetupCustomFormatWithScores("Optional CF", "optional-cf", ("default", 300));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "default-cf",
                    Name = "Default CF",
                    Default = true,
                },
                new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Config: No select list (empty) - use baseline behavior
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "test-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Assert: Plan contains required-cf + default-cf; excludes optional-cf
        plan.CustomFormats.Select(x => x.Resource.TrashId)
            .Should()
            .BeEquivalentTo("required-cf", "default-cf");
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_selecting_required_cf_emits_warning()
    {
        // Setup: Group with Required CF
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        // Config: Select = ["required-cf"] - redundant selection
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Select = ["required-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Assert: CF is included AND warning is emitted
        plan.CustomFormats.Should()
            .ContainSingle()
            .Which.Resource.TrashId.Should()
            .Be("required-cf");
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("redundant")));
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_invalid_select_trash_id_reports_error()
    {
        SetupCustomFormatWithScores("Valid CF", "valid-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "valid-cf", Name = "Valid CF" }],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Select = ["nonexistent-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s =>
                    s.Contains("nonexistent-cf") && s.Contains("Invalid CF trash_id")
                )
            );
    }

    [Test]
    public void Build_with_cf_group_profile_not_in_include_list_reports_error()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData(
            "not-included-qp",
            "Not Included Profile",
            ("HDTV-1080p", true, null)
        );
        SetupQualityProfileGuideData("other-qp", "Other Profile", ("HDTV-1080p", true, null));

        // Group's include list does NOT contain not-included-qp
        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "cf1", Name = "CF One" }],
            new Dictionary<string, string> { ["Other Profile"] = "other-qp" }
        );

        // User explicitly assigns to a profile not in the include list
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { TrashId = "not-included-qp" },
                        ],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "not-included-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s =>
                    s.Contains("not-included-qp") && s.Contains("not in this group's include list")
                )
            );
    }

    [Test]
    public void Build_with_cf_group_invalid_assign_scores_to_trash_id_reports_error()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("valid-qp", "Valid Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "cf1", Name = "CF One" }],
            new Dictionary<string, string> { ["Valid Profile"] = "valid-qp" }
        );

        // User assigns to a profile that doesn't exist
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { TrashId = "nonexistent-profile" },
                        ],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "valid-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s =>
                    s.Contains("nonexistent-profile") && s.Contains("Invalid profile trash_id")
                )
            );
    }

    [Test]
    public void Build_with_cf_group_assign_to_custom_profile_by_name()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("guide-qp", "Guide Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Guide Profile"] = "guide-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { Name = "My Custom Profile" },
                        ],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "guide-qp" },
                new QualityProfileConfig { Name = "My Custom Profile" },
            ],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // CF should be assigned to the custom profile
        var customProfile = plan.QualityProfiles.Single(p => p.Name == "My Custom Profile");
        customProfile.CfScores.Should().ContainSingle();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_mixed_trash_id_and_name_assignment()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("guide-qp", "Guide Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Guide Profile"] = "guide-qp" }
        );

        // Assign to both a guide profile (trash_id) and a custom profile (name)
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { TrashId = "guide-qp" },
                            new CfGroupAssignScoresToConfig { Name = "Custom Profile" },
                        ],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "guide-qp" },
                new QualityProfileConfig { Name = "Custom Profile" },
            ],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // CF should be assigned to both profiles
        plan.QualityProfiles.Should().HaveCount(2).And.OnlyContain(p => p.CfScores.Count == 1);
    }

    [Test]
    public void Build_with_cf_group_assign_to_nonexistent_custom_profile_reports_error()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "cf1", Name = "CF One" }],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        AssignScoresTo =
                        [
                            new CfGroupAssignScoresToConfig { Name = "Nonexistent Profile" },
                        ],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s =>
                    s.Contains("Nonexistent Profile") && s.Contains("No quality profile")
                )
            );
    }

    [Test]
    public void Build_with_cf_group_auto_sync_does_not_include_custom_profiles()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("guide-qp", "Guide Profile", ("HDTV-1080p", true, null));

        // Default group that auto-syncs
        SetupCfGroupGuideData(
            "default-group",
            "Default Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Guide Profile"] = "guide-qp" },
            isDefault: true
        );

        // Config with only a custom profile (no guide-backed profiles)
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { Name = "My Custom Profile" }],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Default groups should NOT auto-sync to custom profiles
        plan.CustomFormats.Should().BeEmpty();
    }

    [Test]
    public void Build_with_cf_group_invalid_exclude_trash_id_reports_error()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Exclude = ["nonexistent-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s =>
                    s.Contains("nonexistent-cf") && s.Contains("Invalid CF trash_id in exclude")
                )
            );
    }

    [Test]
    public void Build_with_cf_group_excluding_required_cf_emits_warning()
    {
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Exclude = ["required-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Required CF is still included despite exclude
        plan.CustomFormats.Should()
            .ContainSingle()
            .Which.Resource.TrashId.Should()
            .Be("required-cf");
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("has no effect")));
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_excluding_non_default_cf_emits_warning()
    {
        SetupCustomFormatWithScores("Optional CF", "optional-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" }],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "test-group",
                        Exclude = ["optional-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher
            .Received()
            .AddWarning(
                Arg.Is<string>(s => s.Contains("optional-cf") && s.Contains("has no effect"))
            );
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_cf_group_selecting_default_cf_emits_warning()
    {
        SetupCustomFormatWithScores("Default CF", "default-cf", ("default", 100));
        SetupQualityProfileGuideData("test-qp", "Test Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "test-group",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "default-cf",
                    Name = "Default CF",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Test Profile"] = "test-qp" }
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig { TrashId = "test-group", Select = ["default-cf"] },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "test-qp" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        // Default CF is still included (redundant selection)
        plan.CustomFormats.Should()
            .ContainSingle()
            .Which.Resource.TrashId.Should()
            .Be("default-cf");
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("redundant")));
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }
}
