using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.MediaNaming;

internal sealed class SonarrNamingPipelineResultTest
{
    [TestCase(0, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 0, SyncResultStatus.Succeeded)]
    [TestCase(1, 1, SyncResultStatus.Partial)]
    [TestCase(0, 1, SyncResultStatus.Failed)]
    public void Status_derives_from_field_completion(
        int completed,
        int incomplete,
        SyncResultStatus expected
    )
    {
        var result = new SonarrNamingPipelineResult(completed, incomplete, [], null);

        result.Status.Should().Be(expected);
    }

    [Test]
    public void Outcomes_are_a_stable_snapshot()
    {
        var outcomes = new List<SonarrNamingOutcome>
        {
            new SonarrNamingReferenceMismatchOutcome(
                SonarrNamingFormatField.StandardEpisodeFormat,
                "unknown"
            ),
        };
        var result = new SonarrNamingPipelineResult(0, 1, outcomes, null);

        outcomes.Clear();

        result.Outcomes.Should().ContainSingle();
    }
}
