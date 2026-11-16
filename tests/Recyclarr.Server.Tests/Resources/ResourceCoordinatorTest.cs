using System.IO.Abstractions;
using Autofac;
using Quartz;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.VersionControl;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Server.Tests.Resources;

internal sealed class ResourceCoordinatorTest : ServerIntegrationFixture
{
    private const string Guides = "trash-guides";
    private const string Templates = "config-templates";

    private readonly TimerObservingTimeProvider _time = new();
    private readonly RecordingSink _log = new();
    private FakeGitRemote _remote = null!;

    private ResourceCoordinator Coordinator => Resolve<ResourceCoordinator>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder
            .RegisterInstance(
                new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(_log).CreateLogger()
            )
            .As<ILogger>();
        builder.RegisterInstance(_time).As<TimeProvider>();
        builder.RegisterType<FakeGitRemote>().As<IRepoUpdater>().AsSelf().SingleInstance();
        builder.RegisterType<FakeGitRemote.Repository>().As<IGitRepository>();
    }

    [SetUp]
    public void SetUp()
    {
        // No config-templates provider is added by default, so includes need a configured one.
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                """
                resource_providers:
                  - name: includes
                    type: config-templates
                    clone_url: https://example.com/includes.git
                """
            )
        );
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
            .SubDirectory("includes")
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
        var fetched = _remote.NextFetch(Guides);

        // The worker creates its timer on another thread; one tick needs that timer to exist.
        _time.Advance(ResourceCoordinator.Ttl);
        await worker.StartAsync(CancellationToken.None);
        await _time.NextTimer(ResourceRefreshWorker.Interval);
        _time.Advance(ResourceRefreshWorker.Interval);
        await fetched;

        // The refresh holds the write lock from fetch to publish, so this read waits until it ends.
        // Stopping the worker first could cancel the refresh before it publishes.
        (await GuideCustomFormats())
            .Should()
            .Equal("cf-one", "cf-two");
        await worker.StopAsync(CancellationToken.None);
    }

    private async Task RunSyncJob(JobId id)
    {
        await Resolve<SyncJobExecution>()
            .Execute(Substitute.For<IJobExecutionContext>(), new SyncJobInput(id.Value));
    }

    [Test]
    public async Task Startup_fails_when_the_configuration_is_invalid()
    {
        AddInstanceUsingInclude("missing-include");

        var act = Startup;

        await act.Should().ThrowAsync<FatalException>();
    }

    private void WriteConfig(string fileName, string yaml) =>
        Fs.AddFile(Paths.ConfigDirectory.File(fileName), new MockFileData(yaml));

    private static string Instance(string name, string apiKey = "asdf") =>
        $"""
            radarr:
              {name}:
                base_url: http://{name}:7878
                api_key: {apiKey}
            """;

    private const string InvalidYaml = "radarr: [unclosed";

    private int ErrorCount => _log.Events.Count(e => e.Level == LogEventLevel.Error);

    [Test]
    public async Task Edited_configuration_is_reloaded_without_a_guide_update()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        WriteConfig("recyclarr.yml", Instance("films"));

        await Background();

        Coordinator.Current.Configuration.InstanceNames.Should().Equal("films");
        _remote.Fetches(Guides).Should().Be(1);
    }

    [Test]
    public async Task Unchanged_inputs_are_not_resolved_again()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        var before = Coordinator.Current;
        var events = _log.Events.Count;

        await Background();

        Coordinator.Current.Should().BeSameAs(before);
        _log.Events.Count.Should().Be(events);
    }

    [Test]
    public async Task Invalid_edit_keeps_the_previous_configuration_and_is_logged_once()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        WriteConfig("recyclarr.yml", InvalidYaml);

        await Background();
        var errorsAfterFirstCheck = ErrorCount;
        await Background();

        Coordinator.Current.Configuration.InstanceNames.Should().Equal("movies");
        errorsAfterFirstCheck.Should().BePositive();
        ErrorCount.Should().Be(errorsAfterFirstCheck);
    }

    [Test]
    public async Task Fixed_edit_after_a_rejection_is_loaded()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        WriteConfig("recyclarr.yml", InvalidYaml);
        await Background();
        WriteConfig("recyclarr.yml", Instance("films"));

        await Background();

        Coordinator.Current.Configuration.InstanceNames.Should().Equal("films");
    }

    [Test]
    public async Task Added_and_deleted_config_files_are_reloaded()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        var extra = Paths.YamlConfigDirectory.File("extra.yml");

        Fs.AddFile(extra, new MockFileData(Instance("films")));
        await Background();
        var namesWithExtra = Coordinator.Current.Configuration.InstanceNames;
        extra.Delete();
        await Background();

        namesWithExtra.Should().BeEquivalentTo("movies", "films");
        Coordinator.Current.Configuration.InstanceNames.Should().Equal("movies");
    }

    [Test]
    public async Task Edited_secrets_are_reloaded()
    {
        WriteConfig("secrets.yml", "movies_key: old");
        WriteConfig("recyclarr.yml", Instance("movies", "!secret movies_key"));
        await Startup();
        WriteConfig("secrets.yml", "movies_key: new");

        await Background();

        Coordinator
            .Current.Configuration.Instances.Should()
            .ContainSingle()
            .Which.ApiKey.Should()
            .Be("new");
    }

    [Test]
    public async Task Edited_file_tag_target_is_reloaded()
    {
        WriteConfig("movies.key", "old");
        WriteConfig("recyclarr.yml", Instance("movies", "!file movies.key"));
        await Startup();
        WriteConfig("movies.key", "new");

        await Background();

        Coordinator
            .Current.Configuration.Instances.Should()
            .ContainSingle()
            .Which.ApiKey.Should()
            .Be("new");
    }

    [Test]
    public async Task Edited_local_include_file_is_checked()
    {
        var include = Paths.YamlIncludeDirectory.File("local.yml");
        Fs.AddFile(include, new MockFileData("quality_definition:\n  type: movie\n"));
        WriteConfig(
            "recyclarr.yml",
            Instance("movies") + "\n    include:\n      - config: local.yml\n"
        );
        await Startup();

        Fs.AddFile(include, new MockFileData(InvalidYaml));
        await Background();

        ErrorCount.Should().BePositive();
    }

    [Test]
    public async Task Reload_message_has_no_config_file_scope()
    {
        AddInstanceUsingInclude("movie-include");
        await Startup();
        WriteConfig(
            "recyclarr.yml",
            Instance("films") + "\n    include:\n      - template: movie-include\n"
        );

        await Background();

        _log.Events.Should()
            .ContainSingle(e =>
                e.MessageTemplate.Text.Contains("reloaded", StringComparison.Ordinal)
            )
            .Which.Properties.Should()
            .NotContainKey("Scope");
    }

    [Test]
    public async Task Edit_waits_while_readers_hold_the_data()
    {
        WriteConfig("recyclarr.yml", Instance("movies"));
        await Startup();
        WriteConfig("recyclarr.yml", Instance("films"));

        using (await Coordinator.AcquireReadAsync(CancellationToken.None))
        {
            await Background();
            Coordinator.Current.Configuration.InstanceNames.Should().Equal("movies");
        }

        await Background();
        Coordinator.Current.Configuration.InstanceNames.Should().Equal("films");
    }

    [Test]
    public async Task Invalid_edit_with_a_guide_update_keeps_both_at_the_last_good_state()
    {
        AddInstanceUsingInclude("movie-include");
        await Startup();
        CommitTemplates("movie-include", "added-include");
        _time.Advance(ResourceCoordinator.Ttl);
        WriteConfig("recyclarr.yml", InvalidYaml);

        await Background();

        Coordinator.Current.Configuration.InstanceNames.Should().Equal("movies");
        var includes = Paths
            .ResourceDirectory.SubDirectory(Templates)
            .SubDirectory("git")
            .SubDirectory("includes")
            .File("includes.json");
        (await Fs.File.ReadAllTextAsync(includes.FullName)).Should().NotContain("added-include");
        _log.Events.Should()
            .NotContain(e =>
                e.Level == LogEventLevel.Warning
                && e.MessageTemplate.Text.Contains("resource provider", StringComparison.Ordinal)
            );
    }

    // Enriches from the log context like the server's logger, so leaked scopes are visible.
    private sealed class RecordingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
