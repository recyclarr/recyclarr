using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

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

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaNaming.Should().NotBeNull();
        publisher.DidNotReceive().AddError(Arg.Any<string>());
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

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher.Received().AddError(Arg.Is<string>(s => s.Contains("nonexistent")));
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

        var (sut, publisher) = CreatePlanBuilder(config);

        sut.Build();

        publisher.Received().AddError(Arg.Any<string>());
    }
}
