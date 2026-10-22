using System.IO.Abstractions;
using Autofac;
using Microsoft.Extensions.Time.Testing;
using Recyclarr.Config;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Telemetry;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.VersionControl;
using TickerQ.Utilities.Base;

namespace Recyclarr.Server.Tests.Resources;

internal sealed class ResourceCoordinatorTest : ServerIntegrationFixture
{
    private const string Guides = "trash-guides";
    private const string Templates = "config-templates";

    private readonly FakeTimeProvider _time = new();
    private FakeGitRemote _remote = null!;

    private ResourceCoordinator Coordinator => Resolve<ResourceCoordinator>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterInstance(_time).As<TimeProvider>();
        builder.RegisterType<FakeGitRemote>().As<IRepoUpdater>().AsSelf().SingleInstance();
        builder.RegisterType<FakeGitRemote.Repository>().As<IGitRepository>();
    }

    [SetUp]
    public void SetUp()
    {
        _remote = Resolve<FakeGitRemote>();
        CommitGuide("cf-one");
        CommitTemplates("movie-include");
    }

    private void CommitGuide(params string[] customFormats)
    {
        var files = new Dictionary<string, string>
        {
            ["metadata.json"] = """
                {
                  "json_paths": {
                    "radarr": { "custom_formats": ["cf"] },
                    "sonarr": {}
                  }
                }
                """,
        };
        foreach (var cf in customFormats)
        {
            files[$"cf/{cf}.json"] = $$"""{ "trash_id": "{{cf}}", "name": "{{cf}}" }""";
        }

        _remote.Commit(Guides, files);
    }

    private void CommitTemplates(params string[] includeIds)
    {
        var entries = includeIds.Select(id => $$"""{ "id": "{{id}}", "template": "inc.yml" }""");
        _remote.Commit(
            Templates,
            new Dictionary<string, string>
            {
                ["includes.json"] = $$"""
                { "radarr": [{{string.Join(",", entries)}}], "sonarr": [] }
                """,
                ["inc.yml"] = "quality_definition:\n  type: movie\n",
            }
        );
    }

    private void AddInstanceUsingInclude(string includeId)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  movies:
                    base_url: http://movies:7878
                    api_key: asdf
                    include:
                      - template: {includeId}
                """
            )
        );
    }

    private async Task<IReadOnlyList<string>> GuideCustomFormats()
    {
        using var lease = await Coordinator.AcquireReadAsync(CancellationToken.None);
        return lease
            .Snapshot.Guide.Files.Get<RadarrCustomFormatResource>()
            .Select(f => Path.GetFileNameWithoutExtension(f.Name))
            .Order()
            .ToList();
    }

    private Task Startup() => Coordinator.RefreshAsync(RefreshMode.Startup, CancellationToken.None);

    private Task Background() =>
        Coordinator.RefreshAsync(RefreshMode.Background, CancellationToken.None);

    [Test]
    public async Task Data_within_the_ttl_is_not_fetched_again()
    {
        await Startup();
        _time.Advance(ResourceCoordinator.Ttl - TimeSpan.FromMinutes(1));

        await Background();

        _remote.Fetches(Guides).Should().Be(1);
    }

    [Test]
    public async Task Expired_data_is_fetched_and_published()
    {
        await Startup();
        CommitGuide("cf-one", "cf-two");
        _time.Advance(ResourceCoordinator.Ttl);

        await Background();

        (await GuideCustomFormats()).Should().Equal("cf-one", "cf-two");
    }

    [Test]
    public async Task Changed_source_identity_is_fetched_before_the_ttl_expires()
    {
        await Startup();
        var stateFile = Paths
            .ResourceDirectory.SubDirectory(Guides)
            .SubDirectory("git")
            .File("official.state.json");
        (ProviderRefreshState.Read(stateFile)! with { SourceIdentity = "old" }).Write(stateFile);

        await Background();

        _remote.Fetches(Guides).Should().Be(2);
    }

    [Test]
    public async Task Refresh_is_skipped_while_readers_hold_the_data()
    {
        await Startup();
        CommitGuide("cf-one", "cf-two");
        _time.Advance(ResourceCoordinator.Ttl);

        using (await Coordinator.AcquireReadAsync(CancellationToken.None))
        {
            await Background();
            _remote.Fetches(Guides).Should().Be(1);
        }

        await Background();
        (await GuideCustomFormats()).Should().Equal("cf-one", "cf-two");
    }

    [Test]
    public async Task Failed_fetch_keeps_cached_data_and_waits_before_retrying()
    {
        await Startup();
        _remote.Offline = true;
        _time.Advance(ResourceCoordinator.Ttl);

        await Background();
        _time.Advance(ResourceCoordinator.FetchBackoff - TimeSpan.FromMinutes(1));
        await Background();
        var fetchesDuringBackoff = _remote.Fetches(Guides);
        _time.Advance(TimeSpan.FromMinutes(1));
        await Background();

        fetchesDuringBackoff.Should().Be(2);
        _remote.Fetches(Guides).Should().Be(3);
        (await GuideCustomFormats()).Should().Equal("cf-one");
    }

    [Test]
    public async Task Update_that_breaks_the_configuration_is_reverted()
    {
        AddInstanceUsingInclude("movie-include");
        await Startup();
        CommitTemplates("renamed-include");
        _time.Advance(ResourceCoordinator.Ttl);

        await Background();

        Coordinator.Current.Configuration.InstanceNames.Should().Equal("movies");
        var includes = Paths
            .ResourceDirectory.SubDirectory(Templates)
            .SubDirectory("git")
            .SubDirectory("official")
            .File("includes.json");
        (await Fs.File.ReadAllTextAsync(includes.FullName)).Should().Contain("movie-include");
    }

    [Test]
    public async Task Breaking_update_without_a_good_mark_keeps_the_current_snapshot()
    {
        AddInstanceUsingInclude("movie-include");
        await Startup();
        var before = Coordinator.Current;
        _remote.ForgetGoodMarks();
        CommitTemplates("renamed-include");
        _time.Advance(ResourceCoordinator.Ttl);

        await Background();

        Coordinator.Current.Should().BeSameAs(before);
    }

    [Test]
    public async Task Sync_run_keeps_its_guide_data_until_it_finishes()
    {
        await Startup();
        CommitGuide("cf-one", "cf-two");
        _time.Advance(ResourceCoordinator.Ttl);
        var fetchesDuringRun = -1;
        Resolve<ISyncOrchestrator>()
            .RunAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(async _ =>
            {
                await Background();
                fetchesDuringRun = _remote.Fetches(Guides);
                return new SyncRunResult([]);
            });
        var job = Resolve<ISyncJobStore>().CreateManualJob([]);

        await RunSyncJob(job.Id);
        await Background();

        fetchesDuringRun.Should().Be(1);
        _remote.Fetches(Guides).Should().Be(2);
    }

    [Test]
    public async Task Background_worker_refreshes_expired_data()
    {
        await Startup();
        CommitGuide("cf-one", "cf-two");
        using var worker = Resolve<ResourceRefreshWorker>();
        await worker.StartAsync(CancellationToken.None);

        // The worker's loop starts on another thread, so its timer may not exist yet; keep ticking.
        _time.Advance(ResourceCoordinator.Ttl);
        for (var i = 0; i < 500 && _remote.Fetches(Guides) < 2; i++)
        {
            _time.Advance(ResourceRefreshWorker.Interval);
            await Task.Delay(10);
        }

        await worker.StopAsync(CancellationToken.None);
        (await GuideCustomFormats()).Should().Equal("cf-one", "cf-two");
    }

    private Task RunSyncJob(JobId id)
    {
        var function = new SyncJobTickerFunction(
            Substitute.For<ILogger>(),
            Resolve<ISyncJobStore>(),
            Coordinator,
            new SyncExecutionGate(),
            Resolve<SyncRunScopeFactory>(),
            Resolve<SyncJobFinalizer>(),
            Resolve<SyncJobTelemetry>()
        );
        var context = new TickerFunctionContext<SyncJobTickerRequest>(
            new TickerFunctionContext(),
            new SyncJobTickerRequest(id.Value)
        );
        return function.ExecuteAsync(context, CancellationToken.None);
    }

    [Test]
    public async Task Startup_fails_when_the_configuration_is_invalid()
    {
        AddInstanceUsingInclude("missing-include");

        var act = Startup;

        await act.Should().ThrowAsync<FatalException>();
    }
}
