using Autofac;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Tests.Pipelines.Plan;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat;

internal sealed class ConfiguredCustomFormatProviderTest : PlanBuilderTestBase
{
    private (ConfiguredCustomFormatProvider Sut, IDiagnosticPublisher Diagnostics) CreateSut(
        IServiceConfiguration config
    )
    {
        var diagnostics = Substitute.For<IDiagnosticPublisher>();
        var scopeFactory = Resolve<LifetimeScopeFactory>();
        var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        return (scope.Resolve<ConfiguredCustomFormatProvider>(), diagnostics);
    }

    [Test]
    public void Auto_discovers_default_groups_when_qp_matches_include_list()
    {
        // Setup: Default CF group with CFs and QP in include list
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: true
        );

        // Config with guide-backed QP (no explicit custom_format_groups)
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // CF should be auto-discovered from default group with implicit source
        entries
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TrashId = "cf1",
                    GroupName = "Default Group",
                    Source = CfSource.CfGroupImplicit,
                    InclusionReason = CfInclusionReason.Default,
                }
            );
    }

    [Test]
    public void No_auto_discovery_when_qp_has_no_trash_id()
    {
        // Setup: Default CF group
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));

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
            new Dictionary<string, string> { ["Some Profile"] = "some-qp" },
            isDefault: true
        );

        // Config with non-guide-backed QP (name only, no trash_id)
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { Name = "User Profile" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // No auto-discovery because QP has no trash_id
        entries.Should().BeEmpty();
    }

    [Test]
    public void No_auto_discovery_when_group_default_is_not_true()
    {
        // Setup: Non-default CF group (Default != "true")
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "non-default-group",
            "Non-Default Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        // Config with guide-backed QP
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // No auto-discovery because group is not default
        entries.Should().BeEmpty();
    }

    [Test]
    public void No_auto_discovery_when_qp_not_in_group_include_list()
    {
        // Setup: Default CF group where user's QP is NOT in include list
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("user-qp", "User Profile", ("HDTV-1080p", true, null));
        SetupQualityProfileGuideData("other-qp", "Other Profile", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["Other Profile"] = "other-qp" },
            isDefault: true
        );

        // Config with QP that's NOT in the group's include list
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "user-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // No auto-discovery because QP is not in include list
        entries.Should().BeEmpty();
    }

    [Test]
    public void Skip_list_excludes_auto_discovered_groups()
    {
        // Setup: Default CF group
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: true
        );

        // Config with skip list that includes the default group
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig { Skip = ["default-group"] },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // No auto-discovery because group is in skip list
        entries.Should().BeEmpty();
    }

    [Test]
    public void CfSource_CfGroupExplicit_for_groups_in_add_list()
    {
        // Setup: Non-default CF group (must be in Add list to sync)
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        // Config explicitly adds the group
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "explicit-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TrashId = "cf1",
                    GroupName = "Explicit Group",
                    Source = CfSource.CfGroupExplicit,
                    InclusionReason = CfInclusionReason.Default,
                }
            );
    }

    [Test]
    public void CfSource_FlatConfig_for_custom_formats_entries()
    {
        // Setup: CF data
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));

        // Config uses flat custom_formats
        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["cf1"] }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TrashId = "cf1",
                    GroupName = (string?)null,
                    Source = CfSource.FlatConfig,
                    InclusionReason = CfInclusionReason.None,
                }
            );
    }

    [Test]
    public void CfSource_ProfileFormatItems_for_qp_formatItems()
    {
        // Setup: QP with formatItems
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileWithFormatItems(
            "anime-qp",
            "Anime Profile",
            "default",
            new Dictionary<string, string> { ["Format Item"] = "cf1" }
        );

        // Config with guide-backed QP that has formatItems
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    TrashId = "cf1",
                    GroupName = (string?)null,
                    Source = CfSource.ProfileFormatItems,
                    InclusionReason = CfInclusionReason.None,
                }
            );
    }

    [Test]
    public void CfInclusionReason_Required_when_cf_is_required()
    {
        // Setup: CF group with required CF
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "explicit-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.InclusionReason.Should()
            .Be(CfInclusionReason.Required);
    }

    [Test]
    public void CfInclusionReason_Selected_when_cf_is_in_select_list()
    {
        // Setup: CF group with optional CF
        SetupCustomFormatWithScores("Optional CF", "optional-cf", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
            [new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" }],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "explicit-group",
                        Select = ["optional-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.InclusionReason.Should()
            .Be(CfInclusionReason.Selected);
    }

    [Test]
    public void Select_adds_non_default_cfs_alongside_defaults()
    {
        // Setup: CF group with one default CF and one non-default CF
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "cf1",
                    Name = "CF One",
                    Default = true,
                },
                new CfGroupCustomFormat { TrashId = "cf2", Name = "CF Two" },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        // Select adds the non-default cf2 alongside the default cf1
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig { TrashId = "explicit-group", Select = ["cf2"] },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Select(e => (e.TrashId, e.InclusionReason))
            .Should()
            .BeEquivalentTo([
                ("cf1", CfInclusionReason.Default),
                ("cf2", CfInclusionReason.Selected),
            ]);
    }

    [Test]
    public void Exclude_removes_specific_default_cfs()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
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
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig { TrashId = "explicit-group", Exclude = ["cf2"] },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries.Should().ContainSingle().Which.TrashId.Should().Be("cf1");
    }

    [Test]
    public void Exclude_does_not_remove_required_cfs()
    {
        SetupCustomFormatWithScores("Required CF", "required-cf", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
            ],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "explicit-group",
                        Exclude = ["required-cf"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.InclusionReason.Should()
            .Be(CfInclusionReason.Required);
    }

    [Test]
    public void Exclude_and_select_compose_correctly()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupCustomFormatWithScores("CF Three", "cf3", ("default", 300));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "explicit-group",
            "Explicit Group",
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
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        // Exclude cf2 (default), select cf3 (non-default) -> result: cf1 + cf3
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "explicit-group",
                        Exclude = ["cf2"],
                        Select = ["cf3"],
                    },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries
            .Select(e => (e.TrashId, e.InclusionReason))
            .Should()
            .BeEquivalentTo([
                ("cf1", CfInclusionReason.Default),
                ("cf3", CfInclusionReason.Selected),
            ]);
    }

    [Test]
    public void Auto_discovered_group_uses_config_profile_name_if_set()
    {
        // Setup: Default CF group
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Guide Profile Name", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["Guide Profile Name"] = "anime-qp" },
            isDefault: true
        );

        // Config with custom name override
        var config = NewConfig.Radarr() with
        {
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "anime-qp", Name = "Custom Name" },
            ],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // AssignScoresTo should use the config's custom name
        entries
            .Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Name.Should()
            .Be("Custom Name");
    }

    [Test]
    public void Optional_only_group_emits_warning()
    {
        // All CFs in this group are optional (no required, no default)
        SetupCustomFormatWithScores("Optional CF", "optional-cf", ("default", 100));
        SetupQualityProfileGuideData("anime-qp", "Anime Profile", ("HDTV-1080p", true, null));

        SetupCfGroupGuideData(
            "optional-group",
            "Optional Miscellaneous",
            [new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" }],
            new Dictionary<string, string> { ["Anime Profile"] = "anime-qp" },
            isDefault: false
        );

        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add = [new CustomFormatGroupConfig { TrashId = "optional-group" }],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries.Should().BeEmpty();
        diagnostics.Received(1).AddWarning(Arg.Is<string>(s => s.Contains("optional-group")));
    }

    [Test]
    public void Default_group_applies_to_all_variant_profiles()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("shared-qp", "WEB-2160p", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["WEB-2160p"] = "shared-qp" },
            isDefault: true
        );

        // Two profiles sharing the same trash_id (variants)
        var config = NewConfig.Radarr() with
        {
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p" },
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p (AMZN DV)" },
            ],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        entries.Should().ContainSingle();
        entries[0]
            .AssignScoresTo.Select(a => a.Name)
            .Should()
            .BeEquivalentTo("WEB-2160p", "WEB-2160p (AMZN DV)");
    }

    [Test]
    public void Explicit_group_assign_scores_to_by_name_targets_specific_variant()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("shared-qp", "WEB-2160p", ("HDTV-1080p", true, null));

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
            new Dictionary<string, string> { ["WEB-2160p"] = "shared-qp" },
            isDefault: false
        );

        // Target only the variant by name
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
                            new AssignScoresToConfig { Name = "WEB-2160p (AMZN DV)" },
                        ],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p" },
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p (AMZN DV)" },
            ],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // Should have entries from the explicit group only targeting the variant
        var explicitEntries = entries.Where(e => e.Source == CfSource.CfGroupExplicit).ToList();
        explicitEntries
            .Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Name.Should()
            .Be("WEB-2160p (AMZN DV)");
    }

    [Test]
    public void Flat_cf_trash_id_with_ambiguous_profiles_reports_error()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("shared-qp", "WEB-2160p", ("HDTV-1080p", true, null));

        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo = [new AssignScoresToConfig { TrashId = "shared-qp" }],
                },
            ],
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p" },
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p (AMZN DV)" },
            ],
        };

        var (sut, diagnostics) = CreateSut(config);

        _ = sut.GetAll(diagnostics).ToList();

        // Should produce an error about ambiguous trash_id
        diagnostics.ReceivedWithAnyArgs(1).AddError(default!);
    }

    [Test]
    public void Flat_cf_assign_scores_to_by_name_targets_specific_profile()
    {
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupQualityProfileGuideData("shared-qp", "WEB-2160p", ("HDTV-1080p", true, null));

        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo = [new AssignScoresToConfig { Name = "WEB-2160p (AMZN DV)" }],
                },
            ],
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p" },
                new QualityProfileConfig { TrashId = "shared-qp", Name = "WEB-2160p (AMZN DV)" },
            ],
        };

        var (sut, diagnostics) = CreateSut(config);

        var entries = sut.GetAll(diagnostics).ToList();

        // Flat CF entries targeting the variant
        var flatEntries = entries.Where(e => e.Source == CfSource.FlatConfig).ToList();
        flatEntries
            .Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Name.Should()
            .Be("WEB-2160p (AMZN DV)");
    }
}
