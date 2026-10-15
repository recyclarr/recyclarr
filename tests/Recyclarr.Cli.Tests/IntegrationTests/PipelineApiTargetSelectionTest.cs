using Autofac;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Compatibility;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class PipelineApiTargetSelectionTest : CliIntegrationFixture
{
    private List<PipelineType> ApplicablePipelines(ServiceApiTarget target)
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());

        return instance
            .Entry.Resolve<IEnumerable<ISyncPipeline>>()
            .Where(p => p.AppliesTo(target))
            .Select(p => p.PipelineType)
            .ToList();
    }

    [Test]
    public void Sonarr_v5_excludes_the_quality_size_pipeline()
    {
        var pipelines = ApplicablePipelines(ServiceApiTarget.SonarrV5);

        pipelines.Should().NotContain(PipelineType.QualitySize);
        pipelines.Should().Contain([PipelineType.CustomFormat, PipelineType.QualityProfile]);
    }

    [Test]
    public void Sonarr_v3_includes_the_quality_size_pipeline()
    {
        var pipelines = ApplicablePipelines(ServiceApiTarget.SonarrV3);

        pipelines.Should().Contain(PipelineType.QualitySize);
    }
}
