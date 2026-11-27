using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Net.Http.Headers;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Recyclarr.Platform;
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
/// CLI's, built by <see cref="CliHost"/>. They are deliberately separate, exactly
/// as they are in production, and share nothing but the HTTP connection between them.
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TUnit owns the fixture's lifetime; an [After(Test)] hook disposes the field"
)]
internal abstract class CliServerHttpFixture : ServerHttpFixture
{
    /// <summary>
    /// Refit needs an absolute base address. The handler behind it routes in-memory, so the
    /// authority is never resolved.
    /// </summary>
    private static readonly Uri ServerAddress = new("http://localhost");

    private readonly CliHost _cli;
    private readonly TestConsole _cliConsole = new TestConsole().Width(200);
    private readonly RecordingLogger _cliLog = new();

    private readonly Lazy<ServerConnection> _connection;

    protected CliServerHttpFixture()
    {
        _cli = new CliHost(RegisterCliStubs);
        _cli.Env.Set("RECYCLARR_CONFIG_DIR", CliConfigDirectory.FullName);

        // A configured server address selects the centralized mode, so commands connect to the
        // in-process server instead of launching an ephemeral one.
        WriteCliSettings(
            $"""
            server:
              base_url: {ServerAddress}
              api_key: {ApiKey}
            """
        );

        _connection = new Lazy<ServerConnection>(() =>
        {
            var client = _cli.Resolve<Func<HttpClient>>()();
            client.BaseAddress = ServerAddress;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                ApiKey
            );
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
    /// Cancels when the running test times out or the test run is aborted.
    /// </summary>
    // non-null: TUnit sets TestContext.Current for the test that is running
    protected static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

    /// <summary>
    /// Writes a config the server can load, so a sync request has something to act on. The server
    /// keeps results only for instances a job selected, so name every instance a stubbed result
    /// reports. Each instance gets its own port because config validation rejects instances that
    /// share a base URL.
    /// </summary>
    protected void AddInstanceConfig(params string[] instanceNames)
    {
        var instances = instanceNames.Select(
            (name, i) => $"""
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
    protected string ConsoleOutput => _cliConsole.Output;

    /// <summary>
    /// The CLI's console. It starts non-interactive and without ANSI control sequences in its
    /// output, like redirected output; tests that need a terminal reconfigure it.
    /// </summary>
    protected TestConsole CliConsole => _cliConsole;

    /// <summary>
    /// Every message the CLI logged, rendered, one per line.
    /// </summary>
    protected string LogOutput => _cliLog.Rendered;

    /// <summary>
    /// Every message the CLI logged, rendered, with the level it was logged at.
    /// </summary>
    protected IReadOnlyList<(LogEventLevel Level, string Message)> LogEntries => _cliLog.Entries;

    private IDirectoryInfo CliConfigDirectory =>
        _cli.Fs.CurrentDirectory().SubDirectory("cli-config");

    /// <summary>
    /// Replaces the CLI's cli.yml. By default it points the CLI at the in-process server.
    /// </summary>
    protected void WriteCliSettings(string yaml)
    {
        _cli.Fs.AddFile(CliConfigDirectory.File("cli.yml"), new MockFileData(yaml));
    }

    /// <summary>
    /// Adds a file to the CLI's file system and returns its full path.
    /// </summary>
    protected string AddCliFile(string name, string content)
    {
        var file = _cli.Fs.CurrentDirectory().File(name);
        _cli.Fs.AddFile(file, new MockFileData(content));
        return file.FullName;
    }

    /// <summary>
    /// Sets an environment variable the CLI reads. The in-process server routes every request
    /// regardless of host, so a test can point the CLI at any address.
    /// </summary>
    protected void SetCliEnvironmentVariable(string name, string value)
    {
        _cli.Env.Set(name, value);
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

    /// <summary>
    /// The CLI sets its own Authorization header from cli.yml or the environment, so the server
    /// fixture's default header is removed to let tests observe what the CLI sends.
    /// </summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = null;
    }

    [After(Test)]
    public void DisposeCli()
    {
        _cli.Dispose();
    }

    private void RegisterCliStubs(ContainerBuilder builder)
    {
        // The production traffic handler sits in front of the in-memory server handler, so tests
        // observe the same request logging as a real run.
        builder.RegisterInstance<Func<HttpClient>>(() =>
            CreateDefaultClient(_cli.Resolve<HttpTrafficLogHandler>())
        );
        builder.RegisterInstance(_cliConsole).As<IAnsiConsole>();
        builder.RegisterInstance(_cliLog).As<ILogger>();
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
