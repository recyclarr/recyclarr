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
    private void SetupQualitySizeGuideData(
        string type,
        params (string Name, decimal Min, decimal Max, decimal Preferred)[] qualities
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var resource = new RadarrQualitySizeResource
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
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", "radarr", "quality-size")
            .File($"{type}.json");
        Fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
        registry.Register<RadarrQualitySizeResource>([file]);
    }

    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        var (sut, _) = CreatePlanBuilder(config);

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

        var (sut, _) = CreatePlanBuilder(config);

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
        var (sut, _) = CreatePlanBuilder(config);

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
        var (sut, _) = CreatePlanBuilder(config);

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
        var (sut, _) = CreatePlanBuilder(config);

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
        var (sut, _) = CreatePlanBuilder(config);

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
        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new PreferredGreaterThanMaxOutcome("Bluray-1080p", Preferred: 75, Max: 50));
    }
}
