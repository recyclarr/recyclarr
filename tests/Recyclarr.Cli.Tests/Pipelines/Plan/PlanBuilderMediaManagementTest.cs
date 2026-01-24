using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Sync.Events;

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

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_null_propers_and_repacks_does_not_add_to_plan()
    {
        var config = NewConfig.Radarr() with
        {
            MediaManagement = new MediaManagementConfig { PropersAndRepacks = null },
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_default_media_management_does_not_add_to_plan()
    {
        // Default MediaManagementConfig has PropersAndRepacks = null
        var config = NewConfig.Radarr();

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        HasErrors(storage).Should().BeFalse();
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

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);
        HasErrors(storage).Should().BeFalse();
    }
}
