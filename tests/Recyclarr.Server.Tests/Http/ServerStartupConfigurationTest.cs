using System.IO.Abstractions;
using System.Net;
using Autofac;
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
