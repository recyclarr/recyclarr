using Recyclarr.Config.Models;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.QualitySize;

internal sealed class QualitySizePipelineResultTest
{
    [TestCase(0, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 1, SyncResultStatus.Partial)]
    [TestCase(0, 1, SyncResultStatus.Failed)]
    public void Status_derives_from_resource_completion(
        int completed,
        int incomplete,
        SyncResultStatus expected
    )
    {
        var result = new QualitySizePipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }

    [Test]
    public void Outcomes_and_delta_components_are_stable_semantic_snapshots()
    {
        var outcomes = new List<QualitySizeOutcome>
        {
            new QualitySizeReferenceMismatchOutcome("Unknown", "movie"),
        };
        var components = new List<QualitySizeUpdateComponent>
        {
            new QualitySizeMinimumChanged(
                new ValueDelta<QualitySizeValue>(
                    new QualitySizeValue.Numeric(5),
                    new QualitySizeValue.Numeric(10)
                )
            ),
        };
        var delta = new QualitySizeDelta("Bluray-1080p", components);
        var deltas = new List<QualitySizeDelta> { delta };
        var result = new QualitySizePipelineResult(1, 1, outcomes, deltas);

        outcomes.Clear();
        components.Clear();
        deltas.Clear();

        result.Outcomes.Should().ContainSingle();
        result.Deltas.Should().ContainSingle().Which.Should().Be(delta);
        delta.Components.Should().ContainSingle();
    }
}
