using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class PipelineOrchestrationIntegrationTest : CliIntegrationFixture
{
    private List<PipelineType> _executionOrder = null!;
    private IInstancePublisher _instancePublisher = null!;
    private Dictionary<PipelineType, IPipelinePublisher> _pipelinePublishers = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _executionOrder = [];
        _pipelinePublishers = [];

        _instancePublisher = Substitute.For<IInstancePublisher>();
        _instancePublisher
            .ForPipeline(Arg.Any<PipelineType>())
            .Returns(ci =>
            {
                var type = ci.Arg<PipelineType>();
                var pub = Substitute.For<IPipelinePublisher>();
                _pipelinePublishers[type] = pub;
                return pub;
            });
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
                Arg.Any<IPipelinePublisher>(),
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
        var cfPipeline = CreateStubPipeline(PipelineType.CustomFormat, []);
        var qpPipeline = CreateStubPipeline(
            PipelineType.QualityProfile,
            [PipelineType.CustomFormat]
        );
        var qsPipeline = CreateStubPipeline(PipelineType.QualitySize, []);
        var mnPipeline = CreateStubPipeline(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([qpPipeline, mnPipeline, cfPipeline, qsPipeline]);

        var settings = Substitute.For<ISyncSettings>();
        await sut.Execute(settings, new PipelinePlan(), _instancePublisher, CancellationToken.None);

        var cfIndex = _executionOrder.IndexOf(PipelineType.CustomFormat);
        var qpIndex = _executionOrder.IndexOf(PipelineType.QualityProfile);
        cfIndex.Should().BeLessThan(qpIndex, "CF must run before QP due to dependency");

        _executionOrder.Should().HaveCount(4);
    }

    [Test]
    public async Task Failed_pipeline_causes_dependents_to_be_skipped()
    {
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

        _executionOrder.Should().NotContain(PipelineType.QualityProfile);

        _executionOrder
            .Should()
            .BeEquivalentTo([
                PipelineType.CustomFormat,
                PipelineType.QualitySize,
                PipelineType.MediaNaming,
            ]);

        // QP should be marked as skipped via its pipeline publisher
        _pipelinePublishers[PipelineType.QualityProfile]
            .Received()
            .SetStatus(PipelineProgressStatus.Skipped, Arg.Any<int?>());

        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task Independent_pipelines_run_even_when_others_fail()
    {
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
