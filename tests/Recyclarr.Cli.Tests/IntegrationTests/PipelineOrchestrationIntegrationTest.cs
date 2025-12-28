using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class PipelineOrchestrationIntegrationTest : CliIntegrationFixture
{
    private List<PipelineType> _executionOrder = null!;
    private ISyncContextSource _contextSource = null!;
    private IProgressSource _progressSource = null!;
    private IServiceConfiguration _config = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _executionOrder = [];
        _contextSource = Substitute.For<ISyncContextSource>();
        _progressSource = Substitute.For<IProgressSource>();
        _config = NewConfig.Radarr() with { InstanceName = "test-instance" };

        builder.RegisterInstance(_contextSource).As<ISyncContextSource>();
        builder.RegisterInstance(_progressSource).As<IProgressSource>();
        builder.RegisterInstance(_config).As<IServiceConfiguration>();
    }

    private ISyncPipeline CreateStubPipeline(
        PipelineType type,
        IReadOnlyList<PipelineType> dependencies,
        PipelineResult result = PipelineResult.Completed
    )
    {
        var pipeline = Substitute.For<ISyncPipeline>();
        pipeline.PipelineType.Returns(type);
        pipeline.Dependencies.Returns(dependencies);
        pipeline
            .Execute(
                Arg.Any<ISyncSettings>(),
                Arg.Any<PipelinePlan>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                _executionOrder.Add(type);
                return Task.FromResult(result);
            });
        return pipeline;
    }

    private IPipelineExecutor CreateExecutor(
        IEnumerable<ISyncPipeline> pipelines,
        IEnumerable<IPipelineCache>? caches = null
    )
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(pipelines).As<IEnumerable<ISyncPipeline>>();
            builder.RegisterInstance(caches ?? []).As<IEnumerable<IPipelineCache>>();
        });
        return scope.Resolve<IPipelineExecutor>();
    }

    [Test]
    public async Task Pipelines_execute_in_topological_order()
    {
        // QP depends on CF; QS and MN are independent
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, []);
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );
        var qsPipeline = CreateStubPipeline(PipelineType.QualitySize, []);
        var mnPipeline = CreateStubPipeline(PipelineType.MediaNaming, []);

        // Register in non-topological order to prove sort works
        var sut = CreateExecutor([qpPipeline, mnPipeline, cfPipeline, qsPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        await sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        // CF must execute before QP (its dependent)
        var cfIndex = _executionOrder.IndexOf(PipelineType.CustomFormat);
        var qpIndex = _executionOrder.IndexOf(PipelineType.QualityProfile);
        cfIndex.Should().BeLessThan(qpIndex, "CF must run before QP due to dependency");

        // All 4 pipelines should have executed
        _executionOrder.Should().HaveCount(4);
    }

    [Test]
    public async Task Failed_pipeline_causes_dependents_to_be_skipped()
    {
        // CF fails, so QP should be skipped. QS and MN are independent and should still run.
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, [], PipelineResult.Failed);
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );
        var qsPipeline = CreateStubPipeline(PipelineType.QualitySize, []);
        var mnPipeline = CreateStubPipeline(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfPipeline, qpPipeline, qsPipeline, mnPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        // QP should NOT have executed (skipped due to CF failure)
        _executionOrder.Should().NotContain(PipelineType.QualityProfile);

        // CF, QS, and MN should have executed
        _executionOrder
            .Should()
            .BeEquivalentTo([
                PipelineType.CustomFormat,
                PipelineType.QualitySize,
                PipelineType.MediaNaming,
            ]);

        // QP should be marked as skipped (context set to QP, then status set to Skipped)
        _contextSource.Received().SetPipeline(PipelineType.QualityProfile);
        _progressSource.Received().SetPipelineStatus(PipelineProgressStatus.Skipped);

        // Overall result should be Failed
        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task Independent_pipelines_run_even_when_others_fail()
    {
        // QS fails, but since nothing depends on it, all other pipelines should still run
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, []);
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );
        var qsPipeline = CreateStubPipeline(PipelineType.QualitySize, [], PipelineResult.Failed);
        var mnPipeline = CreateStubPipeline(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfPipeline, qpPipeline, qsPipeline, mnPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        // All 4 pipelines should have executed (QS failure doesn't affect others)
        _executionOrder.Should().HaveCount(4);
        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task All_successful_returns_completed()
    {
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, []);
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );

        var sut = CreateExecutor([cfPipeline, qpPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        result.Should().Be(PipelineResult.Completed);
    }

    [Test]
    public void Circular_dependency_throws_InvalidOperationException()
    {
        // CF depends on QP, QP depends on CF - circular dependency
        var cfPipeline = CreateStubPipeline(
            PipelineType.CustomFormat,
            [PipelineType.QualityProfile]
        );
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );

        var sut = CreateExecutor([cfPipeline, qpPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        var act = () => sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cycle*");
    }

    [Test]
    public async Task Caches_cleared_before_pipeline_execution()
    {
        var cache1 = Substitute.For<IPipelineCache>();
        var cache2 = Substitute.For<IPipelineCache>();
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, []);

        var sut = CreateExecutor([cfPipeline], [cache1, cache2]);

        var settings = Substitute.For<ISyncSettings>();
        await sut.Execute(settings, new PipelinePlan(), CancellationToken.None);

        cache1.Received(1).Clear();
        cache2.Received(1).Clear();
    }
}
