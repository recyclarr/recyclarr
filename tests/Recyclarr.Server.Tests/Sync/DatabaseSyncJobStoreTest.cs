using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Progress;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Sync;

// Runs against a real (in-memory) SQLite database through the production store.
internal sealed class DatabaseSyncJobStoreTest : ServerIntegrationFixture
{
    private static readonly ServerSyncSettings Settings = new(null, [], Preview: false);
    private static readonly DateTimeOffset Occurrence = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Scheduled_occurrence_without_active_jobs_is_pending()
    {
        var store = Resolve<ISyncJobStore>();

        var job = store.CreateScheduled(Settings, ["movies"], Occurrence);

        job.Status.Should().Be(SyncJobStatus.Pending);
        job.Trigger.Should().Be(SyncJobTrigger.Scheduled);
        job.ScheduledFor.Should().Be(Occurrence);
        job.SkippedBy.Should().BeNull();
    }

    [TestCase(SyncJobStatus.Pending)]
    [TestCase(SyncJobStatus.Running)]
    public void Scheduled_occurrence_during_an_active_job_is_skipped(SyncJobStatus activeStatus)
    {
        var store = Resolve<ISyncJobStore>();
        var active = store.Create(Settings, ["movies"]);
        store.Update(active.Id, job => job.Status = activeStatus);

        var skipped = store.CreateScheduled(Settings, ["movies"], Occurrence);

        skipped.Status.Should().Be(SyncJobStatus.Skipped);
        skipped.SkippedBy.Should().Be(active.Id);
        skipped.FinishedAt.Should().NotBeNull();
        skipped
            .Progress.Instances.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(InstanceProgressStatus.NotRun);
    }

    [Test]
    public void Scheduled_occurrence_after_jobs_finish_is_pending()
    {
        var store = Resolve<ISyncJobStore>();
        Finish(store, store.Create(Settings, []).Id, SyncJobStatus.Succeeded);

        store.CreateScheduled(Settings, [], Occurrence).Status.Should().Be(SyncJobStatus.Pending);
    }

    [Test]
    public async Task Concurrent_occurrences_admit_exactly_one_job()
    {
        var store = Resolve<ISyncJobStore>();

        var jobs = await Task.WhenAll(
            Enumerable
                .Range(0, 20)
                .Select(_ => Task.Run(() => store.CreateScheduled(Settings, [], Occurrence)))
        );

        jobs.Should().ContainSingle(job => job.Status == SyncJobStatus.Pending);
        jobs.Where(job => job.Status != SyncJobStatus.Pending)
            .Should()
            .AllSatisfy(job => job.Status.Should().Be(SyncJobStatus.Skipped));
    }

    [TestCase(SyncJobStatus.Pending, SyncJobStatus.Running)]
    [TestCase(SyncJobStatus.Pending, SyncJobStatus.Interrupted)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Succeeded)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Partial)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Failed)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Interrupted)]
    public void Allowed_transition_is_persisted(SyncJobStatus from, SyncJobStatus to)
    {
        var store = Resolve<ISyncJobStore>();
        var job = store.Create(Settings, []);
        Reach(store, job.Id, from);

        store.Update(job.Id, x => x.Status = to);

        store.Get(job.Id)?.Status.Should().Be(to);
    }

    [TestCase(SyncJobStatus.Pending, SyncJobStatus.Succeeded)]
    [TestCase(SyncJobStatus.Pending, SyncJobStatus.Failed)]
    [TestCase(SyncJobStatus.Pending, SyncJobStatus.Skipped)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Pending)]
    [TestCase(SyncJobStatus.Running, SyncJobStatus.Skipped)]
    public void Rejected_transition_throws_and_persists_nothing(
        SyncJobStatus from,
        SyncJobStatus to
    )
    {
        var store = Resolve<ISyncJobStore>();
        var job = store.Create(Settings, []);
        Reach(store, job.Id, from);

        var act = () => store.Update(job.Id, x => x.Status = to);

        act.Should().Throw<InvalidOperationException>();
        store.Get(job.Id)?.Status.Should().Be(from);
    }

    [TestCase(SyncJobStatus.Succeeded)]
    [TestCase(SyncJobStatus.Interrupted)]
    public void Terminal_job_is_never_mutated(SyncJobStatus terminal)
    {
        var store = Resolve<ISyncJobStore>();
        var job = store.Create(Settings, ["movies"]);
        Finish(store, job.Id, terminal);

        store.Update(job.Id, x => x.Status = SyncJobStatus.Running);

        store.Get(job.Id)?.Status.Should().Be(terminal);
    }

    [Test]
    public void Skipped_job_is_never_mutated()
    {
        var store = Resolve<ISyncJobStore>();
        store.Create(Settings, []);
        var skipped = store.CreateScheduled(Settings, [], Occurrence);

        store.Update(skipped.Id, x => x.Status = SyncJobStatus.Running);

        store.Get(skipped.Id)?.Status.Should().Be(SyncJobStatus.Skipped);
    }

    [Test]
    public void Interrupting_stops_active_jobs_and_leaves_finished_jobs_alone()
    {
        var store = Resolve<ISyncJobStore>();
        var pending = store.Create(Settings, ["waiting"]);
        var running = store.Create(Settings, ["running"]);
        store.Update(
            running.Id,
            job =>
            {
                job.Status = SyncJobStatus.Running;
                job.Progress = job.Progress.Start("running");
            }
        );
        var finished = store.Create(Settings, []);
        Finish(store, finished.Id, SyncJobStatus.Succeeded);

        store.InterruptActive();

        store.Get(pending.Id)?.Status.Should().Be(SyncJobStatus.Interrupted);
        store
            .Get(pending.Id)
            ?.Progress.Instances.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(InstanceProgressStatus.NotRun);
        store
            .Get(running.Id)
            ?.Progress.Instances.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(InstanceProgressStatus.Interrupted);
        store.Get(finished.Id)?.Status.Should().Be(SyncJobStatus.Succeeded);
    }

    [Test]
    public void Retention_evicts_oldest_terminal_jobs_but_never_active_ones()
    {
        var store = Resolve<ISyncJobStore>();
        var active = store.Create(Settings, []);
        var terminal = Enumerable
            .Range(0, 51)
            .Select(_ =>
            {
                var job = store.Create(Settings, []);
                Finish(store, job.Id, SyncJobStatus.Succeeded);
                return job.Id;
            })
            .ToList();

        store.Get(active.Id).Should().NotBeNull();
        store.Get(terminal[0]).Should().BeNull();
        store.GetAll(SyncJobStatus.Succeeded, null).Should().HaveCount(50);
    }

    [Test]
    public void Jobs_are_listed_oldest_first_and_filtered_by_trigger()
    {
        var store = Resolve<ISyncJobStore>();
        var manual = store.Create(Settings, []);
        var scheduled = store.CreateScheduled(Settings, [], Occurrence);

        store.GetAll(null, null).Select(x => x.Id).Should().Equal(manual.Id, scheduled.Id);
        store.GetAll(null, SyncJobTrigger.Scheduled).Select(x => x.Id).Should().Equal(scheduled.Id);
    }

    // Moves a new (Pending) job to an active status through allowed transitions.
    private static void Reach(ISyncJobStore store, JobId id, SyncJobStatus active)
    {
        if (active == SyncJobStatus.Running)
        {
            store.Update(id, x => x.Status = SyncJobStatus.Running);
        }
    }

    // Moves a new (Pending) job to a terminal status through allowed transitions.
    private static void Finish(ISyncJobStore store, JobId id, SyncJobStatus terminal)
    {
        store.Update(id, x => x.Status = SyncJobStatus.Running);
        store.Update(id, x => x.Status = terminal);
    }
}
