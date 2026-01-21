using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Sync.Events;

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

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(2);
        plan.CustomFormats.Select(x => x.Resource.TrashId).Should().BeEquivalentTo("cf1", "cf2");
        HasErrors(storage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_trash_ids_reports_diagnostics()
    {
        SetupCustomFormatGuideData(("Valid CF", "valid-cf"));

        var config = NewConfig.Radarr() with
        {
            CustomFormats = [new CustomFormatConfig { TrashIds = ["valid-cf", "invalid-cf"] }],
        };

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().HaveCount(1);
        GetWarnings(storage).Should().Contain(w => w.Contains("invalid-cf"));
    }

    [Test]
    public void Build_with_no_config_produces_empty_plan()
    {
        var config = NewConfig.Radarr();

        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();
        var storage = Resolve<SyncEventStorage>();

        var plan = sut.Build();

        plan.CustomFormats.Should().BeEmpty();
        HasErrors(storage).Should().BeFalse();
    }
}
