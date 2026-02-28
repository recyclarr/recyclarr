using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

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

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagement.Should().NotBeNull();
        plan.MediaManagement!.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_null_propers_and_repacks_does_not_add_to_plan()
    {
        var config = NewConfig.Radarr() with
        {
            MediaManagement = new MediaManagementConfig { PropersAndRepacks = null },
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagement.Should().BeNull();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Build_with_default_media_management_does_not_add_to_plan()
    {
        // Default MediaManagementConfig has PropersAndRepacks = null
        var config = NewConfig.Radarr();

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagement.Should().BeNull();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
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

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.MediaManagement.Should().NotBeNull();
        plan.MediaManagement!.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }
}
