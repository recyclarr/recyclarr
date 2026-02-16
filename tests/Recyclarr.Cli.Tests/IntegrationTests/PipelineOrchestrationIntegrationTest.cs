using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class PipelineOrchestrationIntegrationTest : CliIntegrationFixture
{
    private List<PipelineType> _executionOrder = null!;
    private ISyncContextSource _contextSource = null!;
    private IProgressSource _progressSource = null!;
    private InstancePublisher _instancePublisher = null!;
    private List<(
        string Instance,
        PipelineType Pipeline,
        PipelineProgressStatus Status
    )> _statusUpdates = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _executionOrder = [];
        _statusUpdates = [];
        _contextSource = Substitute.For<ISyncContextSource>();
        _progressSource = Substitute.For<IProgressSource>();
        _instancePublisher = new InstancePublisher(
            "test-instance",
            Substitute.For<ISyncRunPublisher>()
        );

        // Capture status updates via the writer delegate
        _progressSource
            .ForPipeline(Arg.Any<string>(), Arg.Any<PipelineType>())
            .Returns(callInfo =>
            {
                var instance = callInfo.ArgAt<string>(0);
                var pipeline = callInfo.ArgAt<PipelineType>(1);
                return new PipelineProgressWriter(
                    (status, _) => _statusUpdates.Add((instance, pipeline, status))
                );
            });

        var config = Substitute.For<IServiceConfiguration>();
        config.InstanceName.Returns("test-instance");

        builder.RegisterInstance(_contextSource).As<ISyncContextSource>();
        builder.RegisterInstance(_progressSource).As<IProgressSource>();
        builder.RegisterInstance(config).As<IServiceConfiguration>();
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
                Arg.Any<PipelineProgressWriter>(),
                Arg.Any<PipelinePublisher>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                _executionOrder.Add(type);
                return Task.FromResult(result);
            });
        return pipeline;
    }

    private IPipelineExecutor CreateExecutor(IEnumerable<ISyncPipeline> pipelines)
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(pipelines).As<IEnumerable<ISyncPipeline>>();
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
        await sut.Execute(settings, new PipelinePlan(), _instancePublisher, CancellationToken.None);

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
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            _instancePublisher,
            CancellationToken.None
        );

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

        // QP should be marked as skipped
        _contextSource.Received().SetPipeline(PipelineType.QualityProfile);
        _statusUpdates
            .Should()
            .Contain(x =>
                x.Pipeline == PipelineType.QualityProfile
                && x.Status == PipelineProgressStatus.Skipped
            );

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
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            _instancePublisher,
            CancellationToken.None
        );

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
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            _instancePublisher,
            CancellationToken.None
        );

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
        var act = () =>
            sut.Execute(settings, new PipelinePlan(), _instancePublisher, CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cycle*");
    }
}
