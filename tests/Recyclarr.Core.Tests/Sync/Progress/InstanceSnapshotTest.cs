using System.Collections.Immutable;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Core.Tests.Sync.Progress;

[TestFixture]
internal sealed class InstanceSnapshotTest
{
    [Test]
    public void DeriveStatus_empty_pipelines_returns_Pending()
    {
        ImmutableDictionary<PipelineType, PipelineSnapshot> empty = [];
        InstanceSnapshot.DeriveStatus(empty).Should().Be(InstanceProgressStatus.Pending);
    }

    [Test]
    public void DeriveStatus_all_succeeded_returns_Succeeded()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded);
        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Succeeded);
    }

    [Test]
    public void DeriveStatus_any_running_returns_Running()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .SetItem(
                PipelineType.QualityProfile,
                new PipelineSnapshot(PipelineProgressStatus.Running, null)
            );

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Running);
    }

    [Test]
    public void DeriveStatus_missing_pipeline_returns_Running()
    {
        // Only 4 of 5 pipelines present (all terminal); instance is still in progress
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .Remove(PipelineType.MediaManagement);

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Running);
    }

    [Test]
    public void DeriveStatus_any_failed_returns_Failed()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .SetItem(
                PipelineType.CustomFormat,
                new PipelineSnapshot(PipelineProgressStatus.Failed, null)
            );

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Failed);
    }

    [Test]
    public void DeriveStatus_any_partial_no_failed_returns_Partial()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .SetItem(
                PipelineType.QualityProfile,
                new PipelineSnapshot(PipelineProgressStatus.Partial, 3)
            );

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Partial);
    }

    [Test]
    public void DeriveStatus_failed_takes_precedence_over_partial()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .SetItem(
                PipelineType.CustomFormat,
                new PipelineSnapshot(PipelineProgressStatus.Failed, null)
            )
            .SetItem(
                PipelineType.QualityProfile,
                new PipelineSnapshot(PipelineProgressStatus.Partial, 2)
            );

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Failed);
    }

    [Test]
    public void DeriveStatus_skipped_pipelines_do_not_affect_outcome()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Succeeded)
            .SetItem(
                PipelineType.QualityProfile,
                new PipelineSnapshot(PipelineProgressStatus.Skipped, null)
            );

        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Succeeded);
    }

    [Test]
    public void DeriveStatus_all_skipped_returns_Failed()
    {
        var pipelines = AllPipelinesWithStatus(PipelineProgressStatus.Skipped);
        InstanceSnapshot.DeriveStatus(pipelines).Should().Be(InstanceProgressStatus.Failed);
    }

    private static ImmutableDictionary<PipelineType, PipelineSnapshot> AllPipelinesWithStatus(
        PipelineProgressStatus status
    )
    {
        return Enum.GetValues<PipelineType>()
            .ToImmutableDictionary(t => t, _ => new PipelineSnapshot(status, null));
    }
}
