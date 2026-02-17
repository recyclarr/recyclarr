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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

        entries
            .Should()
            .ContainSingle()
            .Which.InclusionReason.Should()
            .Be(CfInclusionReason.Selected);
    }

    [Test]
    public void Add_with_select_overrides_default_cfs()
    {
        // Setup: CF group with two default CFs
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

        // Config with select list - only cf1 should be included
        var config = NewConfig.Radarr() with
        {
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig { TrashId = "explicit-group", Select = ["cf1"] },
                ],
            },
            QualityProfiles = [new QualityProfileConfig { TrashId = "anime-qp" }],
        };

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

        // Only cf1 (selected) should be included; cf2 (default but not selected) excluded
        entries.Should().ContainSingle().Which.TrashId.Should().Be("cf1");
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

        var scopeFactory = Resolve<LifetimeScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(c =>
        {
            c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
            c.RegisterType<TestConfigurationScope>();
        });
        var sut = scope.Resolve<ConfiguredCustomFormatProvider>();

        var entries = sut.GetAll(IInstancePublisher.Noop).ToList();

        // AssignScoresTo should use the config's custom name
        entries
            .Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Name.Should()
            .Be("Custom Name");
    }
}
