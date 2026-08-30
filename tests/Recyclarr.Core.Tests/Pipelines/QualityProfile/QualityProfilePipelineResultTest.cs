using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfilePipelineResultTest
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
        var result = new QualityProfilePipelineResult(completed, incomplete, [], []);

        result.Status.Should().Be(expected);
    }

    [Test]
    public void Create_and_update_deltas_are_stable_semantic_snapshots()
    {
        var identity = new UserDefinedQualityProfileIdentity("WEB-1080p");
        var qualities = new List<QualityProfileQualityLayoutItem>
        {
            new QualityProfileQuality("Bluray-1080p", true),
        };
        var scores = new List<QualityProfileCustomFormatScore> { new("HDR", "trash-id", 100) };
        var state = new QualityProfileControlledState(
            "WEB-1080p",
            true,
            "Bluray-1080p",
            1000,
            0,
            1,
            "English",
            qualities,
            scores
        );
        var components = new List<QualityProfileUpdateComponent>
        {
            new QualityProfileNameChanged(new ValueDelta<string>("Old", "WEB-1080p")),
            new QualityProfileQualityLayoutChanged(qualities, qualities),
            new QualityProfileCustomFormatScoreChanged(
                "HDR",
                "trash-id",
                new ValueDelta<int>(0, 100),
                QualityProfileScoreChangeReason.Set
            ),
        };
        var create = new QualityProfileCreateDelta(identity, state);
        var update = new QualityProfileUpdateDelta(identity, components);
        var deltas = new List<QualityProfileDelta> { create, update };
        var result = new QualityProfilePipelineResult(2, 0, [], deltas);

        qualities.Clear();
        scores.Clear();
        components.Clear();
        deltas.Clear();

        result.Deltas.Should().Equal(create, update);
        state.Qualities.Should().ContainSingle();
        state.CustomFormatScores.Should().ContainSingle();
        update.Components.Should().HaveCount(3);
    }

    [Test]
    public void Outcome_collections_are_stable_snapshots()
    {
        var identity = new UserDefinedQualityProfileIdentity("WEB-1080p");
        var matches = new List<QualityProfileServiceMatch> { new("WEB-1080p", 1) };
        var names = new List<string> { "Unknown" };
        var ambiguous = new QualityProfileAmbiguousMatchOutcome(identity, matches);
        var mismatch = new QualityProfileQualityReferenceMismatchOutcome(identity, names);
        var outcomes = new List<QualityProfileOutcome> { ambiguous, mismatch };
        var result = new QualityProfilePipelineResult(0, 1, outcomes, []);

        matches.Clear();
        names.Clear();
        outcomes.Clear();

        result.Outcomes.Should().Equal(ambiguous, mismatch);
        ambiguous.ServiceMatches.Should().ContainSingle();
        mismatch.Names.Should().ContainSingle();
    }
}
