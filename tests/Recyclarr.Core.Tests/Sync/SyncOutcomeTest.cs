using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class SyncOutcomeTest
{
    [Test]
    public void Only_instance_blocking_errors_from_any_pipeline_block_the_instance()
    {
        var plan = new PipelinePlan();
        var warning = new PreferredRatioClampedOutcome(Original: 2, Clamped: 1);
        var localError = new InvalidNamingFormatOutcome("Movie Folder Format", "missing");

        plan.Add(new PlannedPipeline<PlannedQualitySizes>(null, [warning, localError]));
        plan.HasInstanceBlockingErrors.Should().BeFalse();

        plan.Add(
            new PlannedPipeline<PlannedMediaManagement>(
                null,
                [new RuleValidationOutcome(SyncDiagnosticLevel.Error, "a", "b", null, "c")]
            )
        );
        plan.HasInstanceBlockingErrors.Should().BeTrue();
    }
}
