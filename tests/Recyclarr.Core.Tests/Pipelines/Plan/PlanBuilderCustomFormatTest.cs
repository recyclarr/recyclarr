using System.IO.Abstractions;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal sealed class PlanBuilderCustomFormatTest : PlanBuilderTestBase
{
    private GuideResourceTestData GuideData => new(Fs, Resolve<ResourceRegistry<IFileInfo>>());

    private void SetupCustomFormatGuideData(params (string Name, string TrashId)[] formats) =>
        GuideData.AddCustomFormats(formats);

    [Test]
    public void Build_with_complete_config_produces_valid_plan()
    {
        SetupCustomFormatGuideData(("Test CF One", "cf1"), ("Test CF Two", "cf2"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["cf1", "cf2"] }],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_invalid_trash_ids_reports_diagnostics()
    {
        SetupCustomFormatGuideData(("Valid CF", "valid-cf"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["valid-cf", "invalid-cf"] }],
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(1);
        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new InvalidCustomFormatTrashIdOutcome("invalid-cf"));
    }

    [Test]
    public void Build_with_no_config_produces_empty_plan()
    {
        var config = NewConfig.Radarr();

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.CustomFormats.Should().BeEmpty();
        plan.Outcomes.Should().BeEmpty();
    }
}
