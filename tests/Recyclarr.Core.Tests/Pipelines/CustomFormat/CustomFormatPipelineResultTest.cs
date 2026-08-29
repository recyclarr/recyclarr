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
    public void Result_retains_typed_outcomes_and_deltas()
    {
        var identity = new CustomFormatIdentity("trash-id", "name");
        CustomFormatOutcome outcome = new CustomFormatAdoptedOutcome(identity, 10);
        CustomFormatDelta delta = new CustomFormatCreateDelta(
            identity,
            new CustomFormatSourceInfo(CfSource.FlatConfig, null, CfInclusionReason.None, [])
        );

        var result = new CustomFormatPipelineResult(1, 0, [outcome], [delta]);

        result.Outcomes.Should().Equal(outcome);
        result.Deltas.Should().Equal(delta);
    }

    [Test]
    public void Update_describes_changed_components_without_service_snapshots()
    {
        var identity = new CustomFormatIdentity("trash-id", "new name");
        CustomFormatUpdateComponent[] components =
        [
            new CustomFormatNameChanged(new ValueDelta<string>("old name", "new name")),
            new CustomFormatSpecificationChanged("Release Title"),
        ];
        var delta = new CustomFormatUpdateDelta(
            identity,
            new CustomFormatSourceInfo(CfSource.FlatConfig, null, CfInclusionReason.None, []),
            components
        );

        delta.Components.Should().Equal(components);
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

        components.Clear();
        serviceMatches.Clear();
        profiles.Clear();

        delta.Components.Should().ContainSingle();
        outcome.ServiceMatches.Should().ContainSingle();
        provenance.ProfileNames.Should().ContainSingle();
    }
}
