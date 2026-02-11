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
internal sealed class PlanBuilderQualitySizeTest(
    ConfigurationScopeFactory scopeFactory,
    ResourceRegistry<IFileInfo> registry,
    SyncEventStorage eventStorage,
    MockFileSystem fs
) : PlanBuilderTestBase(scopeFactory, registry, eventStorage, fs)
{
    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        HasErrors(EventStorage).Should().BeFalse();
    }

    [Test]
    public void Build_with_invalid_quality_type_reports_error()
    {
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "nonexistent" },
        };

        using var scope = ScopeFactory.Start<TestConfigurationScope>(config);
        var sut = scope.Resolve<PlanBuilder>();

        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        GetErrors(EventStorage).Should().Contain(e => e.Contains("nonexistent"));
    }
}
