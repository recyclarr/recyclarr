using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Recyclarr.Server.Features.Sync.GetSchedule;
using Recyclarr.Server.Features.Sync.ListJobs;
using Recyclarr.Server.Persistence;
using Recyclarr.Server.Sync;
using Recyclarr.Server.TestLibrary;
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

    private readonly FakeTimeProvider _time = new(Start);
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

    [Test]
    public async Task Occurrence_while_idle_runs_a_scheduled_job()
    {
        AddInstance();
        using var client = CreateClient();
        var api = RestService.For<ISyncApi>(client);
        (await GetSchedule(client)).NextOccurrence.Should().Be(Midnight);

        _time.SetUtcNow(Midnight);
        var job = await WaitForScheduledJob(client);
        var finished = await WaitForTerminal(api, job.Id);

        finished.Status.Should().Be(nameof(SyncJobStatus.Succeeded));
        finished.ScheduledFor.Should().Be(Midnight);
        var schedule = await GetSchedule(client);
        schedule.PreviousOccurrence.Should().Be(Midnight);
        schedule.NextOccurrence.Should().Be(Midnight.AddDays(1));
    }

    [Test]
    public async Task Occurrence_during_an_active_job_is_skipped()
    {
        AddInstance();
        using var client = CreateClient();
        var active = Services
            .GetRequiredService<ISyncJobStore>()
            .Create(new ServerSyncSettings(null, [], Preview: false), ["movies"]);

        _time.SetUtcNow(Midnight);
        var skipped = await WaitForScheduledJob(client);
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

        _time.Advance(TimeSpan.FromDays(3));
        await WaitForScheduledJob(client);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        (await ListScheduledJobs(client)).Should().ContainSingle();
        (await GetSchedule(client)).NextOccurrence.Should().Be(Midnight.AddDays(3));
    }

    [Test]
    public async Task Disabled_schedule_creates_no_jobs()
    {
        AddSettings("    enabled: false\n");
        AddInstance();
        using var client = CreateClient();

        var schedule = await GetSchedule(client);
        _time.Advance(TimeSpan.FromDays(2));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

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
        _time.Advance(TimeSpan.FromDays(2));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

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

    private static async Task<SyncJobSummaryResponse> WaitForScheduledJob(HttpClient client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var jobs = await ListScheduledJobs(client);
            if (jobs.Count > 0)
            {
                return jobs[0];
            }

            await Task.Delay(50, timeout.Token);
        }
    }

    private static async Task<Recyclarr.Client.V1.GetSyncJobResponse> WaitForTerminal(
        ISyncApi api,
        Guid id
    )
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var job = await api.JobsGet(id, timeout.Token);
            if (job.StatusCode == HttpStatusCode.OK)
            {
                return job.Content!;
            }

            await Task.Delay(50, timeout.Token);
        }
    }
}
