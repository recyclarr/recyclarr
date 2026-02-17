using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class PlanBuilderCustomFormatTest : PlanBuilderTestBase
{
    [Test]
    public void Build_with_complete_config_produces_valid_plan()
    {
        SetupCustomFormatGuideData(("Test CF One", "cf1"), ("Test CF Two", "cf2"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["cf1", "cf2"] }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");
        publisher.DidNotReceive().AddError(Arg.Any<string>());
    }

    [Test]
    public void Build_with_invalid_trash_ids_reports_diagnostics()
    {
        SetupCustomFormatGuideData(("Valid CF", "valid-cf"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["valid-cf", "invalid-cf"] }],
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(1);
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("invalid-cf")));
    }

    [Test]
    public void Build_with_no_config_produces_empty_plan()
    {
        var config = NewConfig.Radarr();

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().BeEmpty();
        publisher.DidNotReceive().AddError(Arg.Any<string>());
    }
}
