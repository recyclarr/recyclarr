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

        var (plan, diagnostics) = sut.Build();

        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");
        diagnostics.ShouldProceed.Should().BeTrue();
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

        var (plan, diagnostics) = sut.Build();

        plan.CustomFormats.Should().HaveCount(1);
        diagnostics.InvalidTrashIds.Should().Contain("invalid-cf");
    }

    [Test]
    public void Build_with_no_config_produces_empty_plan()
    {
        var config = NewConfig.Radarr();

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var (plan, diagnostics) = sut.Build();

        plan.CustomFormats.Should().BeEmpty();
        diagnostics.ShouldProceed.Should().BeTrue();
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

        var (plan, diagnostics) = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        diagnostics.ShouldProceed.Should().BeTrue();
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

        var (plan, diagnostics) = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        diagnostics.Errors.Should().Contain(e => e.Contains("nonexistent"));
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
            CfScores = [new PlannedCfScore(cf, 100)],
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

        var (plan, diagnostics) = sut.Build();

        plan.MediaNaming.Should().NotBeNull();
        diagnostics.InvalidNamingFormats.Should().BeEmpty();
        diagnostics.ShouldProceed.Should().BeTrue();
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

        var (_, diagnostics) = sut.Build();

        diagnostics.InvalidNamingFormats.Should().ContainSingle();
        diagnostics.InvalidNamingFormats.First().ConfigValue.Should().Be("nonexistent");
    }

    [Test]
    public void Build_with_invalid_media_naming_does_not_block_other_pipelines()
    {
        // Invalid media naming should only affect media naming pipeline,
        // not block other pipelines (CF, QP, QualitySize)
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

        var (_, diagnostics) = sut.Build();

        diagnostics.InvalidNamingFormats.Should().NotBeEmpty();
        diagnostics.ShouldProceed.Should().BeTrue();
    }
}
