using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class InMemorySyncJobStoreTest
{
    [Test]
    public void Creation_retains_ordered_pending_instances_and_supports_empty_selection()
    {
        var store = new InMemorySyncJobStore();

        var populated = store.Create(NewSettings(), ["first", "second"]);
        var empty = store.Create(NewSettings(), []);

        populated
            .Progress.Instances.Should()
            .SatisfyRespectively(
                instance =>
                    instance
                        .Should()
                        .BeEquivalentTo(
                            new
                            {
                                Name = "first",
                                Status = InstanceProgressStatus.Pending,
                                Result = (SyncInstanceResult?)null,
                            }
                        ),
                instance =>
                    instance
                        .Should()
                        .BeEquivalentTo(
                            new
                            {
                                Name = "second",
                                Status = InstanceProgressStatus.Pending,
                                Result = (SyncInstanceResult?)null,
                            }
                        )
            );
        empty.Progress.Instances.Should().BeEmpty();
    }

    [Test]
    public void Reads_are_stable_while_valid_transitions_retain_exact_completed_result()
    {
        var store = new InMemorySyncJobStore();
        var created = store.Create(NewSettings(), ["first"]);
        var pending = store.Get(created.Id)!;
        var result = new SyncInstanceResult("first", SupportedServices.Radarr, []);

        store.Update(created.Id, job => job.Progress = job.Progress.Start("first"));
        store.Update(created.Id, job => job.Progress = job.Progress.Complete(result));

        pending.Progress.Instances[0].Status.Should().Be(InstanceProgressStatus.Pending);
        var completed = store.Get(created.Id)!.Progress.Instances.Should().ContainSingle().Which;
        completed.Status.Should().Be(InstanceProgressStatus.Succeeded);
        completed.Result.Should().BeSameAs(result);
    }

    [Test]
    public void Invalid_updates_do_not_regress_or_overwrite_completed_entries()
    {
        var store = new InMemorySyncJobStore();
        var created = store.Create(NewSettings(), ["first"]);
        var firstResult = new SyncInstanceResult("first", SupportedServices.Radarr, []);
        var replacement = new SyncInstanceResult(
            "first",
            SupportedServices.Radarr,
            [],
            fault: new SyncFault("replacement")
        );

        store.Update(
            created.Id,
            job =>
            {
                job.Progress = job.Progress.Start("unknown");
                job.Progress = job.Progress.Complete(firstResult);
                job.Progress = job.Progress.Start("first");
                job.Progress = job.Progress.Complete(firstResult);
                job.Progress = job.Progress.Complete(replacement);
            }
        );

        var instance = store.Get(created.Id)!.Progress.Instances.Should().ContainSingle().Which;
        instance.Status.Should().Be(InstanceProgressStatus.Succeeded);
        instance.Result.Should().BeSameAs(firstResult);
    }

    [Test]
    public void Stop_interrupts_running_instances_and_marks_pending_instances_not_run()
    {
        var store = new InMemorySyncJobStore();
        var created = store.Create(NewSettings(), ["completed", "running", "pending"]);
        var result = new SyncInstanceResult("completed", SupportedServices.Sonarr, []);

        store.Update(
            created.Id,
            job =>
            {
                job.Progress = job.Progress.Start("completed").Complete(result);
                job.Progress = job.Progress.Start("running");
                job.Progress = job.Progress.Stop();
            }
        );

        store
            .Get(created.Id)!
            .Progress.Instances.Select(instance => instance.Status)
            .Should()
            .Equal(
                InstanceProgressStatus.Succeeded,
                InstanceProgressStatus.Interrupted,
                InstanceProgressStatus.NotRun
            );
    }

    [TestCase(SyncResultStatus.Succeeded, InstanceProgressStatus.Succeeded)]
    [TestCase(SyncResultStatus.Partial, InstanceProgressStatus.Partial)]
    [TestCase(SyncResultStatus.Failed, InstanceProgressStatus.Failed)]
    public void Completion_status_is_derived_from_the_retained_result(
        SyncResultStatus resultStatus,
        InstanceProgressStatus expected
    )
    {
        var store = new InMemorySyncJobStore();
        var created = store.Create(NewSettings(), ["instance"]);
        var result = CreateResult(resultStatus);

        store.Update(
            created.Id,
            job => job.Progress = job.Progress.Start("instance").Complete(result)
        );

        var instance = store.Get(created.Id)!.Progress.Instances.Should().ContainSingle().Which;
        instance.Status.Should().Be(expected);
        instance.Result.Should().BeSameAs(result);
    }

    private static ServerSyncSettings NewSettings() => new(null, [], Preview: false, []);

    private static SyncInstanceResult CreateResult(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => new SyncInstanceResult(
                "instance",
                SupportedServices.Radarr,
                []
            ),
            SyncResultStatus.Partial => new SyncInstanceResult(
                "instance",
                SupportedServices.Radarr,
                [new TestPipelineResult(SyncResultStatus.Succeeded)],
                fault: new SyncFault("fault")
            ),
            SyncResultStatus.Failed => new SyncInstanceResult(
                "instance",
                SupportedServices.Radarr,
                [],
                fault: new SyncFault("fault")
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    private sealed record TestPipelineResult : PipelineResult
    {
        public TestPipelineResult(SyncResultStatus status)
            : base(status) { }

        internal override PipelineResult WithStatus(
            SyncResultStatus status,
            PipelineType? blockedBy = null
        ) => new TestPipelineResult(status);
    }
}
