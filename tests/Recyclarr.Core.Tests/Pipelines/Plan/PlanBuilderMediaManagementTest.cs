using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal sealed class PlanBuilderMediaManagementTest : PlanBuilderTestBase
{
    [Test]
    public void Build_with_configured_propers_and_repacks_adds_to_plan()
    {
        var config = NewConfig.Radarr() with
        {
            MediaManagement = new MediaManagementConfig
            {
                PropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_null_propers_and_repacks_does_not_add_to_plan()
    {
        var config = NewConfig.Radarr() with
        {
            MediaManagement = new MediaManagementConfig { PropersAndRepacks = null },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_default_media_management_does_not_add_to_plan()
    {
        // Default MediaManagementConfig has PropersAndRepacks = null
        var config = NewConfig.Radarr();

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_sonarr_and_configured_propers_and_repacks_adds_to_plan()
    {
        var config = NewConfig.Sonarr() with
        {
            MediaManagement = new MediaManagementConfig
            {
                PropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);
        plan.Outcomes.Should().BeEmpty();
    }
}
