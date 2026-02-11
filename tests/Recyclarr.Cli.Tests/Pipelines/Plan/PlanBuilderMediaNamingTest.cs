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
internal sealed class PlanBuilderMediaNamingTest(
    ConfigurationScopeFactory scopeFactory,
    ResourceRegistry<IFileInfo> registry,
    SyncEventStorage eventStorage,
    MockFileSystem fs
) : PlanBuilderTestBase(scopeFactory, registry, eventStorage, fs)
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

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.MediaNaming.Should().NotBeNull();
        HasErrors(EventStorage).Should().BeFalse();
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

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        sut.Build();

        GetErrors(EventStorage).Should().Contain(e => e.Contains("nonexistent"));
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

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        sut.Build();

        HasErrors(EventStorage).Should().BeTrue();
    }
}
