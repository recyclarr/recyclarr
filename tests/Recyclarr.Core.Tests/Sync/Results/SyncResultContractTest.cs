using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using PipelineType = Recyclarr.Sync.PipelineType;

namespace Recyclarr.Core.Tests.Sync.Results;

internal sealed class SyncResultContractTest
{
    [TestCase(SyncResultStatus.Succeeded, true)]
    [TestCase(SyncResultStatus.Partial, false)]
    [TestCase(SyncResultStatus.Failed, false)]
    [TestCase(SyncResultStatus.Blocked, false)]
    public void Only_success_satisfies_dependencies(SyncResultStatus status, bool expected) =>
        status.SatisfiesDependency().Should().Be(expected);

    [Test]
    public void No_op_success_satisfies_dependencies()
    {
        var result = new TestPipelineResult(SyncResultStatus.Succeeded);

        result.Status.SatisfiesDependency().Should().BeTrue();
    }

    [Test]
    public void Blocked_pipeline_identifies_its_direct_dependency()
    {
        var result = new TestPipelineResult(SyncResultStatus.Blocked, PipelineType.CustomFormat);

        result.BlockedBy.Should().Be(PipelineType.CustomFormat);
        result.Status.SatisfiesDependency().Should().BeFalse();
    }

    [TestCase(SyncResultStatus.Succeeded)]
    [TestCase(SyncResultStatus.Partial)]
    [TestCase(SyncResultStatus.Failed)]
    public void Non_blocked_pipeline_cannot_identify_a_blocking_dependency(SyncResultStatus status)
    {
        var act = () => new TestPipelineResult(status, PipelineType.CustomFormat);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Blocked_pipeline_requires_a_blocking_dependency()
    {
        var act = () => new TestPipelineResult(SyncResultStatus.Blocked);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Blocked_pipeline_blocks_its_dependents_transitively()
    {
        var direct = new TestPipelineResult(SyncResultStatus.Blocked, PipelineType.CustomFormat);
        var transitive = new TestPipelineResult(
            SyncResultStatus.Blocked,
            PipelineType.QualityProfile
        );

        direct.Status.SatisfiesDependency().Should().BeFalse();
        transitive.Status.SatisfiesDependency().Should().BeFalse();
    }

    [TestCaseSource(nameof(InstanceStatusCases))]
    public void Instance_status_derives_from_pipeline_results(
        IReadOnlyList<SyncResultStatus> pipelineStatuses,
        OperationalFailure? failure,
        SyncResultStatus expected
    )
    {
        var pipelines = pipelineStatuses
            .Select(x => new TestPipelineResult(
                x,
                x is SyncResultStatus.Blocked ? PipelineType.CustomFormat : null
            ))
            .ToList();

        var result = new SyncInstanceResult("sonarr", SupportedServices.Sonarr, pipelines, failure);

        result.Status.Should().Be(expected);
    }

    [TestCaseSource(nameof(RunStatusCases))]
    public void Run_status_derives_from_instance_results(
        IReadOnlyList<SyncResultStatus> instanceStatuses,
        SyncFault? fault,
        SyncResultStatus expected
    )
    {
        var instances = instanceStatuses.Select(CreateInstance).ToList();

        var result = new SyncRunResult(instances, fault);

        result.Status.Should().Be(expected);
    }

    [Test]
    public void Results_retain_order_and_failure_at_the_owning_boundary()
    {
        var first = new TestPipelineResult(SyncResultStatus.Succeeded);
        var second = new TestPipelineResult(SyncResultStatus.Failed);
        var failure = new ServiceUnavailableFailure();
        var instance = new SyncInstanceResult(
            "radarr",
            SupportedServices.Radarr,
            [first, second],
            failure
        );
        var fault = new SyncFault("fault-reference");

        var run = new SyncRunResult([instance], fault);

        run.Instances.Should().Equal(instance);
        run.Fault.Should().Be(fault);
        instance.Pipelines.Should().Equal(first, second);
        instance.Failure.Should().Be(failure);
    }

    [Test]
    public void Semantic_contracts_are_presentation_free()
    {
        PipelineOutcome outcome = new TestOutcome();
        ResourceDelta resourceDelta = new TestResourceDelta();
        var valueDelta = new ValueDelta<int>(Current: 1, Desired: 2);

        outcome.Should().BeOfType<TestOutcome>();
        resourceDelta.Should().BeOfType<TestResourceDelta>();
        valueDelta.Should().Be(new ValueDelta<int>(Current: 1, Desired: 2));
    }

    [Test]
    public void Operational_failure_variants_are_typed()
    {
        OperationalFailure[] failures =
        [
            new ServiceUnavailableFailure(),
            new ServiceUnauthenticatedFailure(),
            new ServiceUnauthorizedFailure(),
            new ServiceRateLimitedFailure(),
        ];

        failures
            .Select(x => x.GetType())
            .Should()
            .Equal(
                typeof(ServiceUnavailableFailure),
                typeof(ServiceUnauthenticatedFailure),
                typeof(ServiceUnauthorizedFailure),
                typeof(ServiceRateLimitedFailure)
            );
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Fault_reference_must_be_opaque_but_nonempty(string reference)
    {
        var act = () => new SyncFault(reference);

        act.Should().Throw<ArgumentException>();
    }

    private static IEnumerable<TestCaseData> InstanceStatusCases()
    {
        yield return Case([], null, SyncResultStatus.Succeeded, "empty success");
        yield return Case(
            [SyncResultStatus.Succeeded, SyncResultStatus.Succeeded],
            null,
            SyncResultStatus.Succeeded,
            "all succeeded"
        );
        yield return Case(
            [SyncResultStatus.Partial],
            null,
            SyncResultStatus.Partial,
            "partial child"
        );
        yield return Case(
            [SyncResultStatus.Succeeded, SyncResultStatus.Failed],
            null,
            SyncResultStatus.Partial,
            "mixed completion"
        );
        yield return Case(
            [SyncResultStatus.Failed, SyncResultStatus.Blocked],
            null,
            SyncResultStatus.Failed,
            "no completion"
        );
        yield return Case([], new ServiceUnavailableFailure(), SyncResultStatus.Failed, "failure");
        yield return Case(
            [SyncResultStatus.Succeeded],
            new ServiceUnavailableFailure(),
            SyncResultStatus.Partial,
            "completion before failure"
        );

        static TestCaseData Case(
            IReadOnlyList<SyncResultStatus> statuses,
            OperationalFailure? failure,
            SyncResultStatus expected,
            string name
        ) => new(statuses, failure, expected) { TestName = $"Instance_status_{name}" };
    }

    private static IEnumerable<TestCaseData> RunStatusCases()
    {
        yield return Case([], null, SyncResultStatus.Succeeded, "empty success");
        yield return Case(
            [SyncResultStatus.Succeeded],
            null,
            SyncResultStatus.Succeeded,
            "all succeeded"
        );
        yield return Case(
            [SyncResultStatus.Partial],
            null,
            SyncResultStatus.Partial,
            "partial child"
        );
        yield return Case(
            [SyncResultStatus.Succeeded, SyncResultStatus.Failed],
            null,
            SyncResultStatus.Partial,
            "mixed completion"
        );
        yield return Case(
            [SyncResultStatus.Failed],
            null,
            SyncResultStatus.Failed,
            "no completion"
        );
        yield return Case([], new SyncFault("fault"), SyncResultStatus.Failed, "fault");
        yield return Case(
            [SyncResultStatus.Succeeded],
            new SyncFault("fault"),
            SyncResultStatus.Partial,
            "completion before fault"
        );

        static TestCaseData Case(
            IReadOnlyList<SyncResultStatus> statuses,
            SyncFault? fault,
            SyncResultStatus expected,
            string name
        ) => new(statuses, fault, expected) { TestName = $"Run_status_{name}" };
    }

    private static SyncInstanceResult CreateInstance(SyncResultStatus status)
    {
        return new SyncInstanceResult(
            "instance",
            SupportedServices.Sonarr,
            [new TestPipelineResult(status)]
        );
    }

    private sealed record TestPipelineResult : PipelineResult
    {
        public TestPipelineResult(SyncResultStatus status, PipelineType? blockedBy = null)
            : base(status, blockedBy) { }
    }

    private sealed record TestOutcome : PipelineOutcome;

    private sealed record TestResourceDelta : ResourceDelta;
}
