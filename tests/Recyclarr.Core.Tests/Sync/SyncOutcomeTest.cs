using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class SyncOutcomeTest
{
    [Test]
    public void Plan_retains_structured_outcomes_and_derives_error_state()
    {
        var plan = new PipelinePlan();
        var warning = new PreferredRatioClampedOutcome(Original: 2, Clamped: 1);
        var error = new InvalidNamingFormatOutcome("Movie Folder Format", "missing");

        plan.Add(warning);
        plan.Add(error);

        plan.Outcomes.Should().Equal(warning, error);
        plan.HasErrors.Should().BeTrue();
    }
}
