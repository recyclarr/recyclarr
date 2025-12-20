using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class PlanBuilderIntegrationTest : CliIntegrationFixture
{
    private void SetupCustomFormatGuideData(params (string Name, string TrashId)[] cfs)
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        foreach (var (name, trashId) in cfs)
        {
            var cf = NewCf.RadarrData(name, trashId);
            var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
            var path = $"/guide/radarr/cf/{trashId}.json";
            Fs.AddFile(path, new MockFileData(json));
            registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);
        }
    }

    private void SetupQualitySizeGuideData(
        string type,
        params (string Name, decimal Min, decimal Max, decimal Preferred)[] qualities
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qs = new RadarrQualitySizeResource
        {
            Type = type,
            Qualities = qualities
                .Select(q => new QualityItem
                {
                    Quality = q.Name,
                    Min = q.Min,
                    Max = q.Max,
                    Preferred = q.Preferred,
                })
                .ToList(),
        };
        var json = JsonSerializer.Serialize(qs, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-size/{type}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualitySizeResource>([Fs.FileInfo.New(path)]);
    }

    private static bool HasErrors(SyncEventStorage storage) =>
        storage.Diagnostics.Any(e => e.Type == DiagnosticType.Error);

    private static IEnumerable<string> GetWarnings(SyncEventStorage storage) =>
        storage.Diagnostics.Where(e => e.Type == DiagnosticType.Warning).Select(e => e.Message);

    private static IEnumerable<string> GetErrors(SyncEventStorage storage) =>
        storage.Diagnostics.Where(e => e.Type == DiagnosticType.Error).Select(e => e.Message);

    [Test]
    public void Build_with_complete_config_produces_valid_plan()
    {
        SetupCustomFormatGuideData(("Test CF One", "cf1"), ("Test CF Two", "cf2"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["cf1", "cf2"] }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_trash_ids_reports_diagnostics()
    {
        SetupCustomFormatGuideData(("Valid CF", "valid-cf"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["valid-cf", "invalid-cf"] }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(1);
        GetWarnings(storage).Should().Contain(w => w.Contains("invalid-cf"));
    }

    [Test]
    public void Build_with_no_config_produces_empty_plan()
    {
        var config = NewConfig.Radarr();

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().BeEmpty();
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_quality_type_reports_error()
    {
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "nonexistent" },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        GetErrors(storage).Should().Contain(e => e.Contains("nonexistent"));
    }

    [Test]
    public void Cf_id_hydration_visible_to_qp_scores()
    {
        var plan = new PipelinePlan();
        var cf = new PlannedCustomFormat(NewCf.Data("Test", "cf1"));
        plan.AddCustomFormat(cf);

        var qp = new PlannedQualityProfile
        {
            Name = "Test Profile",
            Config = new QualityProfileConfig { Name = "Test Profile" },
            ShouldCreate = false,
            CfScores = [new PlannedCfScore(cf, Score: 100)],
        };
        plan.AddQualityProfile(qp);

        // Simulate persistence setting the ID
        cf.Resource.Id = 42;

        // QP score sees hydrated ID via reference (ServiceId must track Resource.Id)
        plan.QualityProfiles.First().CfScores.First().ServiceId.Should().Be(42);
    }

    private void SetupMediaNamingGuideData(
        IReadOnlyDictionary<string, string>? folderFormats = null,
        IReadOnlyDictionary<string, string>? fileFormats = null
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var naming = new RadarrMediaNamingResource
        {
            Folder = folderFormats ?? new Dictionary<string, string> { ["default"] = "{Movie}" },
            File = fileFormats ?? new Dictionary<string, string> { ["standard"] = "{Movie}.{ext}" },
        };
        var json = JsonSerializer.Serialize(naming, GlobalJsonSerializerSettings.Guide);
        var path = "/guide/radarr/naming/radarr-naming.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrMediaNamingResource>([Fs.FileInfo.New(path)]);
    }

    [Test]
    public void Build_with_valid_media_naming_produces_plan()
    {
        SetupMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "standard", Rename = true },
            },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.MediaNaming.Should().NotBeNull();
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_media_naming_reports_diagnostics()
    {
        SetupMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "nonexistent", Rename = true },
            },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        sut.Build();

        GetErrors(storage).Should().Contain(e => e.Contains("nonexistent"));
    }

    [Test]
    public void Build_with_invalid_media_naming_blocks_sync()
    {
        // Invalid media naming reports an error, which blocks sync
        SetupMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "nonexistent", Rename = true },
            },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        sut.Build();

        HasErrors(storage).Should().BeTrue();
    }

    private void SetupQualityProfileGuideData(
        string trashId,
        string name,
        params (string Name, bool Allowed, string[]? Items)[] qualities
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qp = new RadarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            Items = qualities
                .Select(q => new QualityProfileQualityItem
                {
                    Name = q.Name,
                    Allowed = q.Allowed,
                    Items = q.Items ?? [],
                })
                .ToList(),
        };
        var json = JsonSerializer.Serialize(qp, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-profiles/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualityProfileResource>([Fs.FileInfo.New(path)]);
    }

    [Test]
    public void Build_with_valid_quality_profile_trash_id_produces_plan()
    {
        SetupQualityProfileGuideData(
            "qp-trash-id",
            "Guide QP Name",
            ("HDTV-1080p", true, null),
            ("WEB 1080p", true, ["WEBDL-1080p", "WEBRip-1080p"])
        );

        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "qp-trash-id" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.QualityProfiles.Should().ContainSingle();
        plan.QualityProfiles.First().Name.Should().Be("Guide QP Name");
        plan.QualityProfiles.First().Resource.Should().NotBeNull();
        plan.QualityProfiles.First().Resource!.TrashId.Should().Be("qp-trash-id");
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_quality_profile_trash_id_reports_warning()
    {
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "nonexistent-qp" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.QualityProfiles.Should().BeEmpty();
        GetWarnings(storage).Should().Contain(w => w.Contains("nonexistent-qp"));
    }

    [Test]
    public void Build_with_quality_profile_consolidates_qualities_from_guide()
    {
        SetupQualityProfileGuideData(
            "qp-with-qualities",
            "Profile With Qualities",
            ("Bluray-1080p", true, null),
            ("WEB 1080p", false, ["WEBDL-1080p", "WEBRip-1080p"])
        );

        // Config has no qualities - should consolidate from guide
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "qp-with-qualities" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        var profile = plan.QualityProfiles.Single();
        profile.Config.Qualities.Should().HaveCount(2);
        profile
            .Config.Qualities.Select(q => q.Name)
            .Should()
            .BeEquivalentTo("Bluray-1080p", "WEB 1080p");
    }

    [Test]
    public void Build_with_score_set_uses_score_from_set()
    {
        SetupCustomFormatGuideData(("Test CF", "cf-with-scores"));

        // Need to setup the CF with score sets - let me use a more direct approach
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var cf = new RadarrCustomFormatResource
        {
            Name = "CF With Score Set",
            TrashId = "cf-score-set",
            TrashScores = new Dictionary<string, int> { ["anime-radarr"] = 150, ["default"] = 50 },
        };
        var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
        var path = "/guide/radarr/cf/cf-score-set.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);

        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["cf-score-set"],
                    AssignScoresTo = [new AssignScoresToConfig { Name = "Test Profile" }],
                },
            ],
            QualityProfiles =
            [
                new QualityProfileConfig { Name = "Test Profile", ScoreSet = "anime-radarr" },
            ],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        var profile = plan.QualityProfiles.Single();
        profile.CfScores.Should().ContainSingle().Which.Score.Should().Be(150); // Score from anime-radarr set, not default 50
    }

    [Test]
    public void Build_with_duplicate_cf_in_profile_warns_on_conflict()
    {
        SetupCustomFormatGuideData(("Duplicate CF", "dup-cf"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["dup-cf"],
                    AssignScoresTo =
                    [
                        new AssignScoresToConfig { Name = "Test Profile", Score = 100 },
                        new AssignScoresToConfig { Name = "Test Profile", Score = 200 },
                    ],
                },
            ],
            QualityProfiles = [new QualityProfileConfig { Name = "Test Profile" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        sut.Build();

        GetWarnings(storage)
            .Should()
            .Contain(w => w.Contains("dup-cf") && w.Contains("conflicting"));
    }

    private void SetupQualityProfileWithFormatItems(
        string trashId,
        string name,
        string trashScoreSet,
        IReadOnlyDictionary<string, string> formatItems
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qp = new RadarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            TrashScoreSet = trashScoreSet,
            FormatItems = formatItems,
        };
        var json = JsonSerializer.Serialize(qp, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-profiles/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualityProfileResource>([Fs.FileInfo.New(path)]);
    }

    private void SetupCustomFormatWithScores(
        string name,
        string trashId,
        params (string ScoreSet, int Score)[] scores
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var cf = new RadarrCustomFormatResource
        {
            Name = name,
            TrashId = trashId,
            TrashScores = scores.ToDictionary(x => x.ScoreSet, x => x.Score),
        };
        var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/cf/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);
    }

    [Test]
    public void Build_with_qp_formatItems_synthesizes_cfs()
    {
        // Setup CFs with default scores that will be referenced by QP formatItems
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));

        // Setup QP with formatItems referencing those CFs
        SetupQualityProfileWithFormatItems(
            "qp-with-format-items",
            "Guide Profile",
            "default",
            new Dictionary<string, string> { ["CF One"] = "cf1", ["CF Two"] = "cf2" }
        );

        // Config only specifies QP, not CFs - CFs should come from formatItems
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "qp-with-format-items" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        // CFs should be synthesized from QP formatItems
        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");

        // CFs should be assigned to the QP with scores from default score set
        var profile = plan.QualityProfiles.Single();
        profile.CfScores.Should().HaveCount(2);
        profile.CfScores.Select(x => x.Score).Should().BeEquivalentTo([100, 200]);
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_qp_formatItems_inherits_trash_score_set()
    {
        // Setup CF with score sets
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var cf = new RadarrCustomFormatResource
        {
            Name = "CF With Scores",
            TrashId = "cf-scores",
            TrashScores = new Dictionary<string, int> { ["anime-radarr"] = 200, ["default"] = 50 },
        };
        var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
        var path = "/guide/radarr/cf/cf-scores.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);

        // Setup QP with formatItems and trash_score_set
        SetupQualityProfileWithFormatItems(
            "qp-anime",
            "Anime Profile",
            "anime-radarr",
            new Dictionary<string, string> { ["CF With Scores"] = "cf-scores" }
        );

        // Config specifies QP without explicit score_set - should inherit from resource
        var config = NewConfig.Radarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "qp-anime" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        // Score should come from anime-radarr set (200), not default (50)
        var profile = plan.QualityProfiles.Single();
        profile.CfScores.Should().ContainSingle().Which.Score.Should().Be(200);
    }

    [Test]
    public void Build_with_qp_formatItems_merges_with_config_cfs()
    {
        // Setup CFs with scores
        SetupCustomFormatWithScores("CF One", "cf1", ("default", 100));
        SetupCustomFormatWithScores("CF Two", "cf2", ("default", 200));
        SetupCustomFormatWithScores("CF Three", "cf3", ("default", 300));

        // Setup QP with formatItems for cf1 and cf2
        SetupQualityProfileWithFormatItems(
            "qp-partial",
            "Guide Profile",
            "default",
            new Dictionary<string, string> { ["CF One"] = "cf1", ["CF Two"] = "cf2" }
        );

        // Config specifies cf2 (overlap) and cf3 (additional) with explicit scores
        var config = NewConfig.Radarr() with
        {
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["cf2", "cf3"],
                    AssignScoresTo =
                    [
                        new AssignScoresToConfig { Name = "Guide Profile", Score = 999 },
                    ],
                },
            ],
            QualityProfiles = [new QualityProfileConfig { TrashId = "qp-partial" }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        // All 3 CFs should be in plan (cf1 from formatItems, cf2 merged, cf3 from config)
        plan.CustomFormats.Should().HaveCount(3);
        plan.CustomFormats.Select(x => x.Resource.TrashId)
            .Should()
            .BeEquivalentTo("cf1", "cf2", "cf3");

        // cf2 should have merged assignments (from both config and formatItems)
        var cf2 = plan.CustomFormats.Single(x => x.Resource.TrashId == "cf2");
        cf2.AssignScoresTo.Should().HaveCount(2);
    }
}
