using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Recyclarr.Server.TestLibrary;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
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
    // Refit needs an absolute base address. The handler behind it routes in-memory, so the
    // authority is never resolved.
    private static readonly Uri ServerAddress = new("http://localhost");

    private readonly CliContainer _cli;
    private readonly Lazy<ServerConnection> _connection;

    protected CliServerHttpFixture()
    {
        _cli = new CliContainer(CreateClient);
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
    /// Writes a config the server can load, so a sync request has something to act on.
    /// </summary>
    protected void AddInstanceConfig(string instanceName)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  {instanceName}:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
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

    private sealed class CliContainer(Func<HttpClient> createClient) : CliIntegrationFixture
    {
        public TestConsole Console { get; } = new TestConsole().Width(200);
        public RecordingLogger Log { get; } = new();

        protected override void RegisterStubsAndMocks(ContainerBuilder builder)
        {
            base.RegisterStubsAndMocks(builder);

            builder.RegisterInstance(createClient);
            builder.RegisterInstance(Console).As<IAnsiConsole>();
            builder.RegisterInstance(Log).As<ILogger>();

            // A configured server address selects the centralized mode, so commands connect to
            // the in-process server instead of launching an ephemeral one.
            var settings = Substitute.For<ISettings<ServerSettings>>();
            settings.Value.Returns(new ServerSettings { BaseUrl = ServerAddress });
            builder.RegisterInstance(settings);
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

        public string Rendered =>
            string.Join(
                Environment.NewLine,
                _events.Select(e => e.RenderMessage(CultureInfo.InvariantCulture))
            );

        public void Write(LogEvent logEvent)
        {
            _events.Enqueue(logEvent);
        }
    }
}
