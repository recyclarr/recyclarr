using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatPipelineResultTest
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
        var result = new CustomFormatPipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }

    [Test]
    public void Nested_result_collections_are_stable_snapshots()
    {
        var identity = new CustomFormatIdentity("trash-id", "name");
        var components = new List<CustomFormatUpdateComponent>
        {
            new CustomFormatSpecificationChanged("Release Title"),
        };
        var serviceMatches = new List<CustomFormatServiceMatch> { new("name", 1) };
        var profiles = new List<string> { "WEB-1080p" };
        var provenance = new CustomFormatSourceInfo(
            CfSource.FlatConfig,
            null,
            CfInclusionReason.None,
            profiles
        );
        var delta = new CustomFormatUpdateDelta(identity, provenance, components);
        var outcome = new CustomFormatAmbiguousMatchOutcome(identity, serviceMatches);
        var outcomes = new List<CustomFormatOutcome> { outcome };
        var deltas = new List<CustomFormatDelta> { delta };
        var result = new CustomFormatPipelineResult(1, 0, outcomes, deltas);

        components.Clear();
        serviceMatches.Clear();
        profiles.Clear();
        outcomes.Clear();
        deltas.Clear();

        result.Outcomes.Should().Equal(outcome);
        result.Deltas.Should().Equal(delta);
        delta.Components.Should().ContainSingle();
        outcome.ServiceMatches.Should().ContainSingle();
        provenance.ProfileNames.Should().ContainSingle();
    }
}
