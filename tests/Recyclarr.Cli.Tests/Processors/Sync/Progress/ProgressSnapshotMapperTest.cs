using System.Collections.Immutable;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Client.V1;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Tests.Processors.Sync.Progress;

internal sealed class ProgressSnapshotMapperTest
{
    private static InstanceSnapshotResponse Instance(
        string status,
        params PipelineSnapshotResponse[] pipelines
    )
    {
        return new InstanceSnapshotResponse
        {
            Name = "instance1",
            Status = status,
            Pipelines = [.. pipelines],
        };
    }

    [Test]
    public void Wire_names_become_the_statuses_the_table_renders_from()
    {
        var snapshot = ProgressSnapshotMapper.ToSnapshot([
            Instance(
                "Partial",
                new PipelineSnapshotResponse
                {
                    Type = "CustomFormat",
                    Status = "Succeeded",
                    Count = 4,
                },
                new PipelineSnapshotResponse { Type = "QualityProfile", Status = "Failed" }
            ),
        ]);

        snapshot
            .Should()
            .BeEquivalentTo(
                new ProgressSnapshot([
                    new InstanceSnapshot(
                        "instance1",
                        InstanceProgressStatus.Partial,
                        new Dictionary<PipelineType, PipelineSnapshot>
                        {
                            [PipelineType.CustomFormat] = new(PipelineProgressStatus.Succeeded, 4),
                            [PipelineType.QualityProfile] = new(
                                PipelineProgressStatus.Failed,
                                null
                            ),
                        }.ToImmutableDictionary()
                    ),
                ])
            );
    }

    [Test]
    public void Item_changes_survive_the_round_trip()
    {
        var snapshot = ProgressSnapshotMapper.ToSnapshot([
            Instance(
                "Succeeded",
                new PipelineSnapshotResponse
                {
                    Type = "CustomFormat",
                    Status = "Succeeded",
                    Changes = new PipelineItemChangesResponse
                    {
                        Created = ["cf1"],
                        Updated = ["cf2"],
                        Deleted = ["cf3"],
                    },
                }
            ),
        ]);

        snapshot
            .Instances[0]
            .Pipelines[PipelineType.CustomFormat]
            .Changes.Should()
            .BeEquivalentTo(new PipelineItemChanges(["cf1"], ["cf2"], ["cf3"]));
    }

    [Test]
    public void Values_this_cli_does_not_know_are_dropped_instead_of_crashing_the_display()
    {
        var snapshot = ProgressSnapshotMapper.ToSnapshot([
            Instance(
                "SomethingNewer",
                new PipelineSnapshotResponse { Type = "SomethingNewer", Status = "Succeeded" },
                new PipelineSnapshotResponse { Type = "CustomFormat", Status = "Succeeded" }
            ),
        ]);

        var instance = snapshot.Instances[0];
        instance.Status.Should().Be(InstanceProgressStatus.Pending);
        instance.Pipelines.Keys.Should().Equal(PipelineType.CustomFormat);
    }
}
