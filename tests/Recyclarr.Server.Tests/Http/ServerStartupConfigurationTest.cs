using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Json;
using Autofac;
using Recyclarr.Server.Features.Sync.CreateJob;
using Recyclarr.Server.TestLibrary;
using Serilog.Events;

namespace Recyclarr.Server.Tests.Http;

// Configuration is loaded once while the host starts (ADR-019). These tests start the real host,
// so a configuration problem surfaces as a startup failure rather than as a request error.
internal sealed class ServerStartupConfigurationTest : ServerHttpFixture
{
    private readonly RecordingLogger _log = new();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_log).As<ILogger>();
    }

    private void AddConfig(string yaml)
    {
        Fs.AddFile(Paths.ConfigDirectory.File("recyclarr.yml"), new MockFileData(yaml));
    }

    [TestCase(
        """
            radarr:
              invalid-instance:
                base_url: http://localhost:7878
                api_key: ""
            """,
        "Invalid instance {Instance}: {Message}",
        TestName = "Invalid instance stops startup"
    )]
    [TestCase(
        """
            radarr:
              duplicate-instance:
                base_url: http://localhost:7878
                api_key: first
            sonarr:
              duplicate-instance:
                base_url: http://localhost:8989
                api_key: second
            """,
        "Duplicate instance: {Instance}",
        TestName = "Duplicate instance stops startup"
    )]
    [TestCase(
        """
            radarr:
              split-one:
                base_url: http://localhost:7878
                api_key: first
              split-two:
                base_url: http://localhost:7878
                api_key: second
            """,
        "Instances {Instances} share the same base URL: {BaseUrl}",
        TestName = "Split instances stop startup"
    )]
    [TestCase(
        """
            radarr:
              parse-failure:
                base_url: http://localhost:7878
                api_key: asdf
                unknown_property: invalid
            """,
        "Config parsing failed in {File}: {Message}",
        TestName = "Parse failure stops startup"
    )]
    public void Invalid_configuration_stops_startup_and_logs_the_problem(
        string yaml,
        string expectedTemplate
    )
    {
        AddConfig(yaml);

        var start = () => CreateClient();

        start.Should().Throw<Exception>();
        _log.Events.Should()
            .Contain(evt =>
                evt.Level == LogEventLevel.Error && evt.MessageTemplate.Text == expectedTemplate
            );
    }

    [Test]
    public void Invalid_schedule_cron_stops_startup()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData("server:\n  schedule:\n    cron: not a cron\n")
        );

        var start = () => CreateClient();

        start.Should().Throw<Exception>();
        _log.Events.Should()
            .Contain(evt =>
                evt.Level == LogEventLevel.Fatal
                && evt.MessageTemplate.Text == "Server startup failed: {Message}"
            );
    }

    [TestCase(
        "server:\n  port: [1\n",
        "settings.yml line 3, column 1: invalid YAML",
        TestName = "Invalid settings YAML stops startup"
    )]
    [TestCase(
        "repository:\n  clone_url: http://x\n",
        "settings.yml line 1, column 1: The `repository` setting has been removed",
        TestName = "Removed setting stops startup"
    )]
    [TestCase(
        "server:\n  prot: 9000\n",
        "settings.yml line 2, column 3: unknown setting 'server.prot'",
        TestName = "Unknown setting stops startup"
    )]
    [TestCase(
        "server:\n  schedule:\n    cron: ''\n",
        "settings.yml line 3, column 5: `server.schedule.cron` must not be empty",
        TestName = "Invalid setting value stops startup"
    )]
    public void Invalid_settings_stop_startup_with_their_position(string yaml, string expected)
    {
        Fs.AddFile(Paths.ConfigDirectory.File("settings.yml"), new MockFileData(yaml));

        var start = () => CreateClient();

        start.Should().Throw<Exception>();
        _log.Events.Should()
            .Contain(evt =>
                evt.Level == LogEventLevel.Fatal
                && evt.MessageTemplate.Text == "Server startup failed: {Message}"
                && evt.RenderMessage(CultureInfo.InvariantCulture)
                    .Contains(expected, StringComparison.Ordinal)
            );
    }

    // Fail closed: a server without a valid key would serve its API to anyone (ADR-027).
    [TestCase(null, null, "RECYCLARR_API_KEY_FILE", TestName = "Missing key stops startup")]
    [TestCase("", null, "RECYCLARR_API_KEY_FILE", TestName = "Empty variable stops startup")]
    [TestCase(null, "", "at least 32", TestName = "Empty key file stops startup")]
    [TestCase("short-key", null, "at least 32", TestName = "Short key stops startup")]
    [TestCase(null, "short-key", "at least 32", TestName = "Short key file stops startup")]
    [TestCase(ApiKey, ApiKey, "both set", TestName = "Variable and file together stop startup")]
    public void Invalid_server_api_key_stops_startup(
        string? key,
        string? fileKey,
        string expectedMessage
    )
    {
        Env.Set("RECYCLARR_API_KEY", key);
        if (fileKey is not null)
        {
            SetKeyFile(fileKey);
        }

        var start = () => CreateClient();

        start.Should().Throw<Exception>();
        _log.Events.Should()
            .Contain(evt =>
                evt.Level == LogEventLevel.Fatal
                && evt.RenderMessage(CultureInfo.InvariantCulture)
                    .Contains(expectedMessage, StringComparison.Ordinal)
            );
    }

    [Test]
    public void Unreadable_key_file_stops_startup()
    {
        Env.Set("RECYCLARR_API_KEY", null);
        Env.Set("RECYCLARR_API_KEY_FILE", Fs.CurrentDirectory().File("missing-key").FullName);

        var start = () => CreateClient();

        start.Should().Throw<Exception>();
        _log.Events.Should()
            .Contain(evt =>
                evt.Level == LogEventLevel.Fatal
                && evt.RenderMessage(CultureInfo.InvariantCulture)
                    .Contains("Cannot read the API key file", StringComparison.Ordinal)
            );
    }

    private void SetKeyFile(string content)
    {
        var file = Fs.CurrentDirectory().File("api-key");
        Fs.AddFile(file, new MockFileData(content));
        Env.Set("RECYCLARR_API_KEY_FILE", file.FullName);
    }

    [Test]
    public async Task Deprecation_is_logged_once_at_startup()
    {
        AddConfig(
            """
            radarr:
              deprecated-setting:
                base_url: http://localhost:7878
                api_key: asdf
                replace_existing_custom_formats: true
            """
        );

        using var client = CreateClient();
        await client.GetAsync(new Uri("/api/v1/sync/jobs", UriKind.Relative));

        _log.Events.Should()
            .ContainSingle(evt =>
                evt.Level == LogEventLevel.Warning
                && evt.MessageTemplate.Text == "[DEPRECATED] {Message}"
            );
    }

    [TestCase("CRON_SCHEDULE", "server.schedule.cron", "#cron-schedule")]
    [TestCase("RECYCLARR_CREATE_CONFIG", "configuration files", "#docker-env-removed")]
    [TestCase("RECYCLARR_LOG_LEVEL", "--log", "#docker-env-removed")]
    public async Task Removed_v8_variable_is_warned_about_and_startup_continues(
        string variable,
        string replacement,
        string anchor
    )
    {
        Env.Set(variable, "set");

        using var client = CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/sync/jobs", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var warning = RemovedVariableWarnings().Should().ContainSingle().Which;
        warning.Level.Should().Be(LogEventLevel.Warning);
        warning
            .RenderMessage(CultureInfo.InvariantCulture)
            .Should()
            .Contain(variable)
            .And.Contain(replacement)
            .And.Contain(anchor);
    }

    [Test]
    public async Task No_removed_variable_warning_when_none_are_set()
    {
        using var client = CreateClient();
        await client.GetAsync(new Uri("/api/v1/sync/jobs", UriKind.Relative));

        RemovedVariableWarnings().Should().BeEmpty();
    }

    private IEnumerable<LogEvent> RemovedVariableWarnings()
    {
        return _log.Events.Where(evt =>
            evt.MessageTemplate.Text.StartsWith("[REMOVED]", StringComparison.Ordinal)
        );
    }

    // Includes resolve through resource providers, so this fails if configuration loads before
    // the providers initialize.
    [Test]
    public async Task Configuration_can_use_includes_from_resource_providers()
    {
        var templates = Fs.CurrentDirectory().SubDirectory("templates");
        Fs.AddFile(
            templates.File("includes.json"),
            new MockFileData(
                """
                {
                  "radarr": [{ "id": "test-include", "template": "include.yml" }],
                  "sonarr": []
                }
                """
            )
        );
        Fs.AddFile(
            templates.File("include.yml"),
            new MockFileData(
                """
                quality_definition:
                  type: movie
                """
            )
        );
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                resource_providers:
                  - name: local-templates
                    type: config-templates
                    path: {templates.FullName}
                """
            )
        );
        AddConfig(
            """
            radarr:
              with-include:
                base_url: http://localhost:7878
                api_key: asdf
                include:
                  - template: test-include
            """
        );

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/sync/jobs", UriKind.Relative),
            new CreateSyncJobRequest { Instances = ["with-include"] }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Test]
    public async Task Server_without_configuration_starts()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/sync/jobs", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<LogEvent> _events = [];

        public IReadOnlyList<LogEvent> Events
        {
            get
            {
                lock (_events)
                {
                    return [.. _events];
                }
            }
        }

        public void Write(LogEvent logEvent)
        {
            lock (_events)
            {
                _events.Add(logEvent);
            }
        }
    }
}
