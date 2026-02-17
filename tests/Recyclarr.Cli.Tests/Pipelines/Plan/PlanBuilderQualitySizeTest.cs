using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class PlanBuilderQualitySizeTest : PlanBuilderTestBase
{
    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        publisher.DidNotReceive().AddError(Arg.Any<string>());
    }

    [Test]
    public void Build_with_invalid_quality_type_reports_error()
    {
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "nonexistent" },
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        publisher.Received().AddError(Arg.Is<string>(s => s.Contains("nonexistent")));
    }
}
