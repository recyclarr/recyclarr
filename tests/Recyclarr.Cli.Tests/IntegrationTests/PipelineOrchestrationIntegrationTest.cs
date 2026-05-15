using Autofac;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.TrashGuide;

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

        var config = Substitute.For<IServiceConfiguration>();
        config.ServiceType.Returns(SupportedServices.Sonarr);
        builder.RegisterInstance(config).As<IServiceConfiguration>();

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

    private ISyncOperation CreateStubOperation(
        PipelineType type,
        IReadOnlyList<PipelineType> dependencies,
        bool shouldFail = false,
        bool shouldSkip = false
    )
    {
        var operation = Substitute.For<ISyncOperation>();
        operation.Type.Returns(type);
        operation.Dependencies.Returns(dependencies);
        operation.ShouldSkip(default!).ReturnsForAnyArgs(shouldSkip);
        operation
            .Compute(
                Arg.Any<PipelinePlan>(),
                Arg.Any<IPipelinePublisher>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                if (shouldFail)
                {
                    throw new PipelineInterruptException();
                }

                _executionOrder.Add(type);
                return Task.CompletedTask;
            });
        return operation;
    }

    private IPipelineExecutor CreateExecutor(IEnumerable<ISyncOperation> ops)
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(ops).As<IEnumerable<ISyncOperation>>();
        });
        return scope.Resolve<IPipelineExecutor>();
    }

    [Test]
    public async Task Pipelines_execute_in_topological_order()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(PipelineType.QualitySize, []);
        var mnOp = CreateStubOperation(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([qpOp, mnOp, cfOp, qsOp]);

        var settings = Substitute.For<ISyncSettings>();
        await sut.Execute(settings, new TestPlan(), _instancePublisher, CancellationToken.None);

        var cfIndex = _executionOrder.IndexOf(PipelineType.CustomFormat);
        var qpIndex = _executionOrder.IndexOf(PipelineType.QualityProfile);
        cfIndex.Should().BeLessThan(qpIndex, "CF must run before QP due to dependency");

        _executionOrder.Should().HaveCount(4);
    }

    [Test]
    public async Task Failed_pipeline_causes_dependents_to_be_skipped()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, [], shouldFail: true);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(PipelineType.QualitySize, []);
        var mnOp = CreateStubOperation(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfOp, qpOp, qsOp, mnOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _instancePublisher,
            CancellationToken.None
        );

        _executionOrder.Should().NotContain(PipelineType.QualityProfile);

        _executionOrder
            .Should()
            .BeEquivalentTo([PipelineType.QualitySize, PipelineType.MediaNaming]);

        // QP should be marked as skipped via its pipeline publisher
        _pipelinePublishers[PipelineType.QualityProfile].ReceivedWithAnyArgs().SetStatus(default);

        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task Independent_pipelines_run_even_when_others_fail()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(PipelineType.QualitySize, [], shouldFail: true);
        var mnOp = CreateStubOperation(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfOp, qpOp, qsOp, mnOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _instancePublisher,
            CancellationToken.None
        );

        // CF, QP, MN should succeed; QS fails (but doesn't get counted in _executionOrder since
        // it throws before adding to the list)
        _executionOrder.Should().HaveCount(3);
        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task All_successful_returns_completed()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);

        var sut = CreateExecutor([cfOp, qpOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _instancePublisher,
            CancellationToken.None
        );

        result.Should().Be(PipelineResult.Completed);
    }

    [Test]
    public async Task Plan_errors_skip_all_pipelines_and_return_Failed()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);

        var sut = CreateExecutor([cfOp, qpOp]);

        var plan = new TestPlan();
        plan.AddError("Simulated plan error");

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(settings, plan, _instancePublisher, CancellationToken.None);

        _executionOrder.Should().BeEmpty("no operations should execute when plan has errors");

        _pipelinePublishers[PipelineType.CustomFormat].ReceivedWithAnyArgs().SetStatus(default);

        _pipelinePublishers[PipelineType.QualityProfile].ReceivedWithAnyArgs().SetStatus(default);

        result.Should().Be(PipelineResult.Failed);
    }

    [Test]
    public async Task Duplicate_pipeline_type_runs_applicable_and_skips_other()
    {
        var applicableOp = CreateStubOperation(PipelineType.MediaNaming, []);
        var skippedOp = CreateStubOperation(PipelineType.MediaNaming, [], shouldSkip: true);

        var sut = CreateExecutor([applicableOp, skippedOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new TestPlan(),
            _instancePublisher,
            CancellationToken.None
        );

        _executionOrder.Should().BeEquivalentTo([PipelineType.MediaNaming]);
        result.Should().Be(PipelineResult.Completed);
    }

    [Test]
    public void Circular_dependency_throws_InvalidOperationException()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, [PipelineType.QualityProfile]);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);

        var sut = CreateExecutor([cfOp, qpOp]);

        var settings = Substitute.For<ISyncSettings>();
        var act = () =>
            sut.Execute(settings, new TestPlan(), _instancePublisher, CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cycle*");
    }
}
