using System.IO.Abstractions;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal sealed class PlanBuilderQualitySizeTest : PlanBuilderTestBase
{
    private static readonly Version SonarrV4 = new("4.0.20.3012");
    private static readonly Version SonarrV5 = new("5.0.0.2569");

    private void SetupQualitySizeGuideData(
        string type,
        params (string Name, decimal Min, decimal Max, decimal Preferred)[] qualities
    )
    {
        SetupQualitySizeGuideData<RadarrQualitySizeResource>(type, qualities);
    }

    private void SetupQualitySizeGuideData<TResource>(
        string type,
        params (string Name, decimal Min, decimal Max, decimal Preferred)[] qualities
    )
        where TResource : QualitySizeResource, new()
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var resource = new TResource
        {
            TrashId = $"qs-{type}",
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
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", typeof(TResource).Name)
            .File($"{type}.json");
        Fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
        registry.Register<TResource>([file]);
    }

    private void SetupSonarrQualityProfileGuideData(
        string trashId,
        string name,
        params (string Label, string TrashId)[] trashQualityDefinition
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var resource = new SonarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            TrashQualityDefinition = trashQualityDefinition.ToDictionary(
                x => x.Label,
                x => x.TrashId
            ),
        };
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", "sonarr", "quality-profiles")
            .File($"{trashId}.json");
        Fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
        registry.Register<SonarrQualityProfileResource>([file]);
    }

    private void SetupSonarrSizeTypes()
    {
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("series", ("HDTV-1080p", 5, 50, 30));
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("anime", ("HDTV-1080p", 2, 20, 10));
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("guide", ("HDTV-1080p", 7, 70, 35));
    }

    private static QualityDefinitionConfig Qd(string type) => new() { Type = type };

    [Test]
    public void Sonarr_v4_plans_global_sizes_without_outcomes()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with { QualityDefinition = Qd("series") };

        var plan = CreatePlanBuilder(config, SonarrV4).Build();

        plan.QualitySizes.Type.Should().Be("series");
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Sonarr_v5_size_only_config_reports_unused_definition_and_plans_nothing()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with { QualityDefinition = Qd("series") };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        plan.Outcomes.Should().Equal(new QualityDefinitionUnusedOutcome());
    }

    [Test]
    public void Sonarr_v5_assigns_sizes_per_profile_by_precedence()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "qs-guide"));
        SetupSonarrQualityProfileGuideData("guide-noref", "Guide NoRef");

        var config = NewConfig.Sonarr() with
        {
            QualityDefinition = Qd("series"),
            QualityProfiles =
            [
                new QualityProfileConfig
                {
                    TrashId = "guide-ref",
                    Name = "Yaml Wins",
                    QualityDefinition = Qd("anime"),
                },
                new QualityProfileConfig { TrashId = "guide-ref" },
                new QualityProfileConfig { TrashId = "guide-noref" },
                new QualityProfileConfig { Name = "User Defined" },
            ],
        };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        plan.Outcomes.Should().BeEmpty();
        plan.QualityProfiles.Should()
            .BeEquivalentTo(
                [
                    new { Name = "Yaml Wins", QualitySizes = new { Type = "anime" } },
                    new { Name = "Guide Ref", QualitySizes = new { Type = "guide" } },
                    new { Name = "Guide NoRef", QualitySizes = new { Type = "series" } },
                    new { Name = "User Defined", QualitySizes = new { Type = "series" } },
                ],
                o => o.ExcludingMissingMembers()
            );
    }

    [Test]
    public void Sonarr_v5_profile_without_any_size_source_gets_none()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { Name = "No Sizes" }],
        };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
    }

    [Test]
    public void Sonarr_v5_unknown_profile_size_type_is_attributed_to_the_profile()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles =
            [
                new QualityProfileConfig { Name = "Bad", QualityDefinition = Qd("missing") },
            ],
        };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.HasInstanceBlockingErrors.Should().BeFalse();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<QualityProfileSizeOutcome>()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    Profile = new { Name = "Bad" },
                    Inner = new QualityDefinitionNotFoundOutcome("missing"),
                },
                o => o.ExcludingMissingMembers()
            );
    }

    [Test]
    public void Sonarr_v5_unknown_guide_size_reference_is_attributed_to_the_profile()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "missing-id"));
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<QualityProfileSizeOutcome>()
            .Which.Inner.Should()
            .Be(new GuideQualityDefinitionNotFoundOutcome("guide", "missing-id"));
    }

    [Test]
    public void Sonarr_v5_guide_reference_with_multiple_entries_is_attributed_to_the_profile()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData(
            "guide-ref",
            "Guide Ref",
            ("series", "qs-series"),
            ("anime", "qs-anime")
        );
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var plan = CreatePlanBuilder(config, SonarrV5).Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<QualityProfileSizeOutcome>()
            .Which.Inner.Should()
            .BeOfType<AmbiguousGuideQualityDefinitionOutcome>()
            .Which.Labels.Should()
            .BeEquivalentTo("series", "anime");
    }

    [Test]
    public void Sonarr_v4_ignores_guide_profile_size_reference_silently()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "qs-guide"));
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var plan = CreatePlanBuilder(config, SonarrV4).Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Sonarr_v4_reports_ignored_profile_size_source_and_keeps_global_sizes()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityDefinition = Qd("series"),
            QualityProfiles =
            [
                new QualityProfileConfig { Name = "Override", QualityDefinition = Qd("anime") },
            ],
        };

        var plan = CreatePlanBuilder(config, SonarrV4).Build();

        plan.QualitySizes.Type.Should().Be("series");
        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ProfileQualityDefinitionIgnoredOutcome>()
            .Which.Profile.Name.Should()
            .Be("Override");
    }

    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_invalid_quality_type_reports_error()
    {
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "nonexistent" },
        };

        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeTrue();
        plan.QualitySizes.Qualities.Should().BeEmpty();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new QualityDefinitionNotFoundOutcome("nonexistent"));
    }

    [Test]
    public void Build_with_out_of_range_ratio_retains_clamped_value()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50));
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie", PreferredRatio = 2 },
        };
        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizes.PreferredRatio.Should().Be(1);
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new PreferredRatioClampedOutcome(Original: 2, Clamped: 1));
    }

    [Test]
    public void Build_with_invalid_size_order_skips_only_invalid_quality()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "movie",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "Bluray-1080p",
                        Min = new QualitySizeValue.Numeric(75),
                        Preferred = new QualitySizeValue.Numeric(50),
                    },
                ],
            },
        };
        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.QualitySizes.Qualities.Should().ContainSingle().Which.Quality.Should().Be("WEB-1080p");
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new MinGreaterThanPreferredOutcome("Bluray-1080p", Min: 75, Preferred: 50));
    }

    [Test]
    public void Build_with_unknown_quality_retains_quality_and_definition_type()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50));
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "movie",
                Qualities = [new QualityDefinitionItemConfig { Name = "Unknown" }],
            },
        };
        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new QualityNotFoundOutcome("Unknown", "movie"));
    }

    [Test]
    public void Build_with_unlimited_preferred_and_finite_max_retains_boundary()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50));
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "movie",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "Bluray-1080p",
                        Max = new QualitySizeValue.Numeric(100),
                        Preferred = new QualitySizeValue.Unlimited(),
                    },
                ],
            },
        };
        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new UnlimitedPreferredGreaterThanMaxOutcome("Bluray-1080p", Max: 100));
    }

    [Test]
    public void Build_with_preferred_greater_than_max_retains_boundary()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50));
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "movie",
                Qualities =
                [
                    new QualityDefinitionItemConfig
                    {
                        Name = "Bluray-1080p",
                        Max = new QualitySizeValue.Numeric(50),
                        Preferred = new QualitySizeValue.Numeric(75),
                    },
                ],
            },
        };
        var sut = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new PreferredGreaterThanMaxOutcome("Bluray-1080p", Preferred: 75, Max: 50));
    }
}
