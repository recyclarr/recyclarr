using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal sealed class PlanBuilderMediaNamingTest : PlanBuilderTestBase
{
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

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.RadarrMediaNaming.Should().NotBeNull();
        plan.Outcomes.Should().BeEmpty();
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

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new InvalidNamingFormatOutcome("Standard Movie Format", "nonexistent"));
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

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
    }
}
