using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using SemanticPipelineResult = Recyclarr.Sync.Results.PipelineResult;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class PipelineOrchestrationIntegrationTest : CoreIntegrationTestFixture
{
    private List<PipelineType> _executionOrder = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _executionOrder = [];

        var config = Substitute.For<IServiceConfiguration>();
        config.ServiceType.Returns(SupportedServices.Sonarr);
        builder.RegisterInstance(config).As<IServiceConfiguration>();
    }

    private IPipeline CreateStubOperation(
        PipelineType type,
        IReadOnlyList<PipelineType> dependencies,
        bool shouldSkip = false,
        SyncResultStatus? structuredStatus = null
    )
    {
        var operation = Substitute.For<IPipeline>();
        operation.Type.Returns(type);
        operation.Dependencies.Returns(dependencies);
        // Each pipeline needs its own plan type; the stubbed HasPlan never reads the plan
        operation.PlanType.Returns(typeof(TestPlanValue).MakeArrayType((int)type + 1));
        operation.HasPlan(default!).ReturnsForAnyArgs(!shouldSkip);
        operation
            .Execute(default, default!, default!, default)
            .ReturnsForAnyArgs(callInfo =>
            {
                _executionOrder.Add(type);
                var result = new TestPipelineResult(structuredStatus ?? SyncResultStatus.Succeeded);
                callInfo.ArgAt<Action<SemanticPipelineResult>>(2)(result);
                return result;
            });
        operation
            .CreateBlockedResult(default)
            .ReturnsForAnyArgs(callInfo => new TestPipelineResult(
                SyncResultStatus.Blocked,
                callInfo.Arg<PipelineType>()
            ));
        operation
            .CreateFailedResult(default)
            .ReturnsForAnyArgs(callInfo =>
                (callInfo.Arg<SemanticPipelineResult?>() as TestPipelineResult)?.WithStatus(
                    SyncResultStatus.Failed
                ) ?? new TestPipelineResult(SyncResultStatus.Failed)
            );
        return operation;
    }

    private IPipelineExecutor CreateExecutor(IEnumerable<IPipeline> pipelines)
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(pipelines).As<IEnumerable<IPipeline>>();
        });
        return scope.Resolve<IPipelineExecutor>();
    }

    private static PipelinePlan PlanWith(SyncOutcome outcome)
    {
        var plan = new PipelinePlan();
        plan.Add(new PlannedPipeline<TestPlanValue>(null, [outcome]));
        return plan;
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
        await sut.Execute(
            settings,
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        var cfIndex = _executionOrder.IndexOf(PipelineType.CustomFormat);
        var qpIndex = _executionOrder.IndexOf(PipelineType.QualityProfile);
        cfIndex.Should().BeLessThan(qpIndex, "CF must run before QP due to dependency");

        _executionOrder.Should().HaveCount(4);
    }

    [Test]
    public async Task Failed_pipeline_causes_dependents_to_be_skipped()
    {
        var cfOp = CreateStubOperation(
            PipelineType.CustomFormat,
            [],
            structuredStatus: SyncResultStatus.Failed
        );
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(PipelineType.QualitySize, []);
        var mnOp = CreateStubOperation(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfOp, qpOp, qsOp, mnOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
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

        result.Should().Contain(x => x.Status == SyncResultStatus.Blocked);
    }

    [TestCase(SyncResultStatus.Partial)]
    [TestCase(SyncResultStatus.Failed)]
    public async Task Unsuccessful_structured_result_blocks_dependents(SyncResultStatus status)
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, [], structuredStatus: status);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);

        var sut = CreateExecutor([cfOp, qpOp]);

        var result = await sut.Execute(
            Substitute.For<ISyncSettings>(),
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().Equal(PipelineType.CustomFormat);
        result.Should().Contain(x => x.Status == SyncResultStatus.Blocked);
    }

    [Test]
    public async Task Blocked_result_blocks_its_dependents_transitively()
    {
        var cfOp = CreateStubOperation(
            PipelineType.CustomFormat,
            [],
            structuredStatus: SyncResultStatus.Failed
        );
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(PipelineType.QualitySize, [PipelineType.QualityProfile]);
        var sut = CreateExecutor([qsOp, qpOp, cfOp]);

        var results = await sut.Execute(
            Substitute.For<ISyncSettings>(),
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().Equal(PipelineType.CustomFormat);
        results.Should().HaveCount(3);
        results[1].Status.Should().Be(SyncResultStatus.Blocked);
        results[1].BlockedBy.Should().Be(PipelineType.CustomFormat);
        results[2].Status.Should().Be(SyncResultStatus.Blocked);
        results[2].BlockedBy.Should().Be(PipelineType.QualityProfile);
    }

    [Test]
    public async Task Unexpected_failure_retains_a_failed_pipeline_snapshot()
    {
        var operation = Substitute.For<IPipeline>();
        operation.Type.Returns(PipelineType.CustomFormat);
        operation.Dependencies.Returns([]);
        operation.PlanType.Returns(typeof(TestPlanValue));
        operation.HasPlan(default!).ReturnsForAnyArgs(true);
        operation
            .Execute(default, default!, default!, default)
            .ReturnsForAnyArgs(
                Task.FromException<SemanticPipelineResult>(
                    new InvalidOperationException("unexpected")
                )
            );
        operation
            .CreateFailedResult(default)
            .ReturnsForAnyArgs(new TestPipelineResult(SyncResultStatus.Failed));
        var buffer = new PipelineExecutionBuffer();
        var sut = CreateExecutor([operation]);

        var act = () =>
            sut.Execute(
                Substitute.For<ISyncSettings>(),
                new PipelinePlan(),
                buffer,
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unexpected");
        buffer.Results.Should().ContainSingle().Which.Status.Should().Be(SyncResultStatus.Failed);
    }

    [Test]
    public async Task Independent_pipelines_run_even_when_others_fail()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var qsOp = CreateStubOperation(
            PipelineType.QualitySize,
            [],
            structuredStatus: SyncResultStatus.Failed
        );
        var mnOp = CreateStubOperation(PipelineType.MediaNaming, []);

        var sut = CreateExecutor([cfOp, qpOp, qsOp, mnOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().HaveCount(4);
        result.Should().Contain(x => x.Status == SyncResultStatus.Failed);
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
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        result.Should().OnlyContain(x => x.Status == SyncResultStatus.Succeeded);
    }

    [Test]
    public async Task Resource_local_plan_errors_are_handled_by_operations()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);

        var sut = CreateExecutor([cfOp, qpOp]);

        var plan = PlanWith(new InvalidNamingFormatOutcome("test", "invalid"));

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            plan,
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.Status == SyncResultStatus.Succeeded);
    }

    [Test]
    public async Task Instance_blocking_plan_errors_fail_without_executing_pipelines()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var qpOp = CreateStubOperation(PipelineType.QualityProfile, [PipelineType.CustomFormat]);
        var sut = CreateExecutor([cfOp, qpOp]);
        var plan = PlanWith(
            new RuleValidationOutcome(
                SyncDiagnosticLevel.Error,
                "test",
                "Simulated plan error",
                null,
                "test"
            )
        );

        var results = await sut.Execute(
            Substitute.For<ISyncSettings>(),
            plan,
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().BeEmpty();
        results
            .Select(x => x.Status)
            .Should()
            .Equal(SyncResultStatus.Failed, SyncResultStatus.Blocked);
        results[1].BlockedBy.Should().Be(PipelineType.CustomFormat);
    }

    [Test]
    public async Task Skipped_operation_runs_nothing_and_reports_no_result()
    {
        var cfOp = CreateStubOperation(PipelineType.CustomFormat, []);
        var skippedOp = CreateStubOperation(PipelineType.MediaNaming, [], shouldSkip: true);

        var sut = CreateExecutor([cfOp, skippedOp]);

        var settings = Substitute.For<ISyncSettings>();
        var result = await sut.Execute(
            settings,
            new PipelinePlan(),
            new PipelineExecutionBuffer(),
            CancellationToken.None
        );

        _executionOrder.Should().Equal(PipelineType.CustomFormat);
        result.Should().ContainSingle();
    }

    private sealed record TestPlanValue;

    private sealed record TestPipelineResult : SemanticPipelineResult
    {
        public TestPipelineResult(SyncResultStatus status, PipelineType? blockedBy = null)
            : base(status, blockedBy) { }

        internal override SemanticPipelineResult WithStatus(
            SyncResultStatus status,
            PipelineType? blockedBy = null
        ) => new TestPipelineResult(status, blockedBy);
    }
}
