using System.IO.Abstractions;
using System.Net.Http.Json;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Server.Features.Sync.GetSchedule;
using Recyclarr.Server.Features.Sync.ListJobs;
using Recyclarr.Server.Hosting;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Server.Tests.Reusable;
using ISyncApi = Recyclarr.Client.V1.ISyncApi;
using RestService = Refit.RestService;

namespace Recyclarr.Server.Tests.Http;

// The scheduler waits on the server's TimeProvider, so advancing fake time fires occurrences. Fake
// time starts in the past: TickerQ runs queued jobs by the real clock. The fake clock's local time
// zone (UTC unless a test sets it) stands in for the system time zone.
internal sealed class ScheduledSyncHttpTest : ServerHttpFixture
{
    private static readonly DateTimeOffset Start = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Midnight = new(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private readonly TimerObservingTimeProvider _time = new(Start);
    private ServerMode _mode = ServerMode.Persistent;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_time).As<TimeProvider>();
        var mode = _mode;
        builder.Register(_ => mode);
    }

    private void AddSettings(string schedule)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData($"server:\n  schedule:\n{schedule}")
        );
    }

    private void AddInstance()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                radarr:
                  movies:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
    }

    [Test]
    public async Task Default_schedule_is_daily_at_midnight()
    {
        using var client = CreateClient();

        var schedule = await GetSchedule(client);

        schedule.Enabled.Should().BeTrue();
        schedule.Cron.Should().Be("0 0 * * *");
        schedule.PreviousOccurrence.Should().BeNull();
        schedule.NextOccurrence.Should().Be(Midnight);
    }

    [Test]
    public async Task System_time_zone_applies_to_the_cron_expression()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        AddSettings("    cron: 30 6 * * *\n");
        using var client = CreateClient();

        var schedule = await GetSchedule(client);

        schedule.TimeZone.Should().Be("America/Chicago");
        schedule
            .NextOccurrence.Should()
            .Be(new DateTimeOffset(2025, 1, 1, 12, 30, 0, TimeSpan.Zero));
    }

    // Whether an occurrence runs or is skipped is decided when its job is created; running a
    // queued job to completion is covered by the CLI's HTTP tests.
    [Test]
    public async Task Occurrence_while_idle_queues_a_scheduled_job()
    {
        AddInstance();
        using var client = CreateClient();
        (await GetSchedule(client)).NextOccurrence.Should().Be(Midnight);

        await SchedulerWait();
        _time.SetUtcNow(Midnight);
        await SchedulerWait();

        var queued = await TheOnlyScheduledJob(client);
        var job = (await RestService.For<ISyncApi>(client).JobsGet(queued.Id)).Content;
        job?.Status.Should().NotBe(nameof(SyncJobStatus.Skipped));
        job?.ScheduledFor.Should().Be(Midnight);
        var schedule = await GetSchedule(client);
        schedule.PreviousOccurrence.Should().Be(Midnight);
        schedule.NextOccurrence.Should().Be(Midnight.AddDays(1));
    }

    [Test]
    public async Task Occurrence_during_an_active_job_is_skipped()
    {
        AddInstance();
        using var client = CreateClient();
        var active = Services.GetRequiredService<ISyncJobStore>().CreateManualJob(["movies"]);

        await SchedulerWait();
        _time.SetUtcNow(Midnight);
        await SchedulerWait();
        var skipped = await TheOnlyScheduledJob(client);
        var job = await RestService.For<ISyncApi>(client).JobsGet(skipped.Id);

        job.Content?.Status.Should().Be(nameof(SyncJobStatus.Skipped));
        job.Content?.ScheduledFor.Should().Be(Midnight);
        job.Content?.SkippedBecause?.JobId.Should().Be(active.Id.Value);
    }

    [Test]
    public async Task Clock_jump_past_several_occurrences_creates_one_job()
    {
        AddInstance();
        using var client = CreateClient();

        await SchedulerWait();
        _time.Advance(TimeSpan.FromDays(3));
        await SchedulerWait();

        (await ListScheduledJobs(client)).Should().ContainSingle();
        (await GetSchedule(client)).NextOccurrence.Should().Be(Midnight.AddDays(3));
    }

    // A disabled schedule never starts the scheduler loop (decided during host startup), so no
    // amount of elapsed time can create a job and the test need not wait for one.
    [Test]
    public async Task Disabled_schedule_creates_no_jobs()
    {
        AddSettings("    enabled: false\n");
        AddInstance();
        using var client = CreateClient();

        var schedule = await GetSchedule(client);

        schedule.Enabled.Should().BeFalse();
        schedule.NextOccurrence.Should().BeNull();
        (await ListScheduledJobs(client)).Should().BeEmpty();
    }

    [Test]
    public async Task Ephemeral_server_never_schedules()
    {
        _mode = ServerMode.Ephemeral;
        AddInstance();
        using var client = CreateClient();

        var schedule = await GetSchedule(client);

        schedule.Enabled.Should().BeFalse();
        (await ListScheduledJobs(client)).Should().BeEmpty();
    }

    private static async Task<SyncScheduleResponse> GetSchedule(HttpClient client)
    {
        var schedule = await client.GetFromJsonAsync<SyncScheduleResponse>(
            new Uri("/api/v1/sync/schedule", UriKind.Relative)
        );
        return schedule!;
    }

    private static async Task<IReadOnlyList<SyncJobSummaryResponse>> ListScheduledJobs(
        HttpClient client
    )
    {
        var list = await client.GetFromJsonAsync<ListSyncJobsResponse>(
            new Uri("/api/v1/sync/jobs?trigger=scheduled", UriKind.Relative)
        );
        return list!.Jobs;
    }

    /// <summary>
    /// Completes when the scheduler next waits on fake time (a one-shot delay). Moving the clock
    /// before the first wait would let the scheduler size that wait from the moved clock and miss
    /// the occurrence. Each occurrence is handled before the next wait, so awaiting a wait after
    /// moving the clock proves the occurrence's job exists.
    /// </summary>
    private Task SchedulerWait() => _time.NextTimer(Timeout.InfiniteTimeSpan);

    private static async Task<SyncJobSummaryResponse> TheOnlyScheduledJob(HttpClient client) =>
        (await ListScheduledJobs(client)).Should().ContainSingle().Subject;
}
