using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Recyclarr.Server.TestLibrary;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Reusable;

/// <summary>
/// Runs Recyclarr.Server in-process and hands the test a real Refit client bound to it, so CLI
/// command handlers are exercised over the same HTTP surface they use in production instead of
/// against a substituted API.
/// </summary>
/// <remarks>
/// Two containers are in play: the server's, built by <see cref="ServerHttpFixture"/>, and the
/// CLI's, built by <see cref="CliIntegrationFixture"/>. They are deliberately separate, exactly
/// as they are in production, and share nothing but the HTTP connection between them.
/// </remarks>
internal abstract class CliServerHttpFixture : ServerHttpFixture
{
    /// <summary>
    /// Refit needs an absolute base address. The handler behind it routes in-memory, so the
    /// authority is never resolved.
    /// </summary>
    private static readonly Uri ServerAddress = new("http://localhost");

    private readonly CliContainer _cli;
    private readonly Lazy<ServerConnection> _connection;

    protected CliServerHttpFixture()
    {
        // The production traffic handler sits in front of the in-memory server handler, so tests
        // observe the same request logging as a real run. non-null: the factory runs only after
        // construction, when a command connects.
        _cli = new CliContainer(() => CreateDefaultClient(_cli!.Resolve<HttpTrafficLogHandler>()));
        _connection = new Lazy<ServerConnection>(() =>
        {
            var client = _cli.Resolve<Func<HttpClient>>()();
            client.BaseAddress = ServerAddress;
            return new ServerConnection(client, ownedServer: null);
        });
    }

    /// <summary>
    /// The CLI's production connection type (and therefore its Refit settings), talking to the
    /// in-process server.
    /// </summary>
    protected ServerConnection Connection => _connection.Value;

    protected ISyncApi Api => Connection.Sync;

    /// <summary>
    /// Writes a config the server can load, so a sync request has something to act on. The server
    /// keeps results only for instances a job selected, so name every instance a stubbed result
    /// reports. Each instance gets its own port because config validation rejects instances that
    /// share a base URL.
    /// </summary>
    protected void AddInstanceConfig(params string[] instanceNames)
    {
        var instances = instanceNames.Select(
            (name, i) =>
                $"""
                      {name}:
                        base_url: http://localhost:{7878 + i}
                        api_key: asdf
                    """
        );

        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData($"radarr:\n{string.Join('\n', instances)}\n")
        );
    }

    protected T ResolveCli<T>()
        where T : notnull
    {
        return _cli.Resolve<T>();
    }

    /// <summary>
    /// Everything the CLI wrote to the console during <see cref="RunCliAsync"/>. Tests run with
    /// redirected output, which puts the CLI in log mode and silences markup output, so only raw
    /// list output reaches it; use <see cref="LogOutput"/> for messages.
    /// </summary>
    protected string ConsoleOutput => _cli.Console.Output;

    /// <summary>
    /// Every message the CLI logged, rendered, one per line.
    /// </summary>
    protected string LogOutput => _cli.Log.Rendered;

    /// <summary>
    /// Every message the CLI logged, rendered, with the level it was logged at.
    /// </summary>
    protected IReadOnlyList<(LogEventLevel Level, string Message)> LogEntries => _cli.Log.Entries;

    /// <summary>
    /// Replaces the CLI's cli.yml. By default it points the CLI at the in-process server.
    /// </summary>
    protected void WriteCliSettings(string yaml)
    {
        _cli.WriteCliSettings(yaml);
    }

    /// <summary>
    /// Sets an environment variable the CLI reads. The in-process server routes every request
    /// regardless of host, so a test can point the CLI at any address.
    /// </summary>
    protected void SetCliEnvironmentVariable(string name, string value)
    {
        _cli.SetEnvironmentVariable(name, value);
    }

    /// <summary>
    /// Runs a CLI command line the way <c>Program</c> does, against the in-process server, and
    /// returns its exit code.
    /// </summary>
    protected async Task<int> RunCliAsync(params string[] args)
    {
        var scope = _cli.Resolve<ILifetimeScope>();
        try
        {
            return await CliSetup.Run(scope, args);
        }
        catch (Exception e)
        {
            if (!await scope.Resolve<ExceptionHandler>().TryHandleAsync(e))
            {
                throw;
            }

            return (int)ExitStatus.Failed;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cli.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class CliContainer : CliIntegrationFixture
    {
        private readonly Func<HttpClient> _createClient;

        public CliContainer(Func<HttpClient> createClient)
        {
            _createClient = createClient;
            Env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns(ConfigDirectory.FullName);

            // A configured server address selects the centralized mode, so commands connect to
            // the in-process server instead of launching an ephemeral one.
            WriteCliSettings(
                $"""
                server:
                  base_url: {ServerAddress}
                """
            );
        }

        public TestConsole Console { get; } = new TestConsole().Width(200);
        public RecordingLogger Log { get; } = new();

        protected override void RegisterStubsAndMocks(ContainerBuilder builder)
        {
            builder.RegisterInstance(_createClient);
            builder.RegisterInstance(Console).As<IAnsiConsole>();
            builder.RegisterInstance(Log).As<ILogger>();
        }

        private IDirectoryInfo ConfigDirectory => Fs.CurrentDirectory().SubDirectory("cli-config");

        public void WriteCliSettings(string yaml)
        {
            Fs.AddFile(ConfigDirectory.File("cli.yml"), new MockFileData(yaml));
        }

        public void SetEnvironmentVariable(string name, string value)
        {
            Env.GetEnvironmentVariable(name).Returns(value);
        }

        public new T Resolve<T>()
            where T : notnull
        {
            return base.Resolve<T>();
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly ConcurrentQueue<LogEvent> _events = new();

        public string Rendered => string.Join(Environment.NewLine, Entries.Select(e => e.Message));

        public IReadOnlyList<(LogEventLevel Level, string Message)> Entries =>
            [.. _events.Select(e => (e.Level, e.RenderMessage(CultureInfo.InvariantCulture)))];

        public void Write(LogEvent logEvent)
        {
            _events.Enqueue(logEvent);
        }
    }
}
