using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Compatibility;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class PipelineApiTargetSelectionTest : CliIntegrationFixture
{
    // Plan errors make the executor mark every applicable pipeline skipped without running it,
    // which exposes the selected pipeline set without contacting a service.
    private async Task<IReadOnlyCollection<PipelineType>> ApplicablePipelines(
        ServiceApiTarget target
    )
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());
        await using var detected = instance.Entry.BeginDetectedServiceScope(
            new DetectedService(new Version(5, 0), target)
        );

        var plan = new TestPlan();
        plan.AddError("forces skip");

        var publisher = Substitute.For<IInstancePublisher>();
        await detected
            .Resolve<IPipelineExecutor>()
            .Execute(Substitute.For<ISyncSettings>(), plan, publisher, CancellationToken.None);

        return publisher.ReceivedCalls().Select(c => (PipelineType)c.GetArguments()[0]!).ToList();
    }

    [Test]
    public async Task Sonarr_v5_excludes_the_quality_size_pipeline()
    {
        var pipelines = await ApplicablePipelines(ServiceApiTarget.SonarrV5);

        pipelines.Should().NotContain(PipelineType.QualitySize);
        pipelines.Should().Contain([PipelineType.CustomFormat, PipelineType.QualityProfile]);
    }

    [Test]
    public async Task Sonarr_v3_includes_the_quality_size_pipeline()
    {
        var pipelines = await ApplicablePipelines(ServiceApiTarget.SonarrV3);

        pipelines.Should().Contain(PipelineType.QualitySize);
    }
}
