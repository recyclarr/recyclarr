using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

[CliDataSource]
internal sealed class PlanBuilderMediaManagementTest(
    ConfigurationScopeFactory scopeFactory,
    ResourceRegistry<IFileInfo> registry,
    SyncEventStorage eventStorage,
    MockFileSystem fs
) : PlanBuilderTestBase(scopeFactory, registry, eventStorage, fs)
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

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotUpgrade);
        HasErrors(EventStorage).Should().BeFalse();
    }

    [Test]
    public void Build_with_null_propers_and_repacks_does_not_add_to_plan()
    {
        var config = NewConfig.Radarr() with
        {
            MediaManagement = new MediaManagementConfig { PropersAndRepacks = null },
        };

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        HasErrors(EventStorage).Should().BeFalse();
    }

    [Test]
    public void Build_with_default_media_management_does_not_add_to_plan()
    {
        // Default MediaManagementConfig has PropersAndRepacks = null
        var config = NewConfig.Radarr();

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeFalse();
        HasErrors(EventStorage).Should().BeFalse();
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

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.MediaManagementAvailable.Should().BeTrue();
        plan.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.PreferAndUpgrade);
        HasErrors(EventStorage).Should().BeFalse();
    }
}
