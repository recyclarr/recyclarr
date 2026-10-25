using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Recyclarr.Platform;
using Recyclarr.Server.Hosting;
using Recyclarr.Server.Logging;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Server.Tests;

// The Serilog file sink writes through the real filesystem, so MockFileSystem cannot observe it.
internal sealed class ServerLoggerTest
{
    private IDirectoryInfo _root = null!;

    [SetUp]
    public void CreateRoot()
    {
        _root = new FileSystem().Directory.CreateTempSubdirectory("recyclarr-server-logger-");
    }

    [TearDown]
    public void DeleteRoot()
    {
        _root.Delete(recursive: true);
    }

    [Test]
    public void Verbose_and_debug_events_are_written_to_separate_files()
    {
        var paths = CreatePaths();

        WriteLogs(
            paths,
            log =>
            {
                log.Verbose("verbose event");
                log.Debug("debug event");
            }
        );

        var files = paths.ServerLogDirectory.GetFiles();
        files
            .Should()
            .ContainSingle(f => f.Name.EndsWith(".verbose.log", StringComparison.Ordinal))
            .Which.ReadAllText()
            .Should()
            .Contain("verbose event")
            .And.NotContain("debug event");
        files
            .Should()
            .ContainSingle(f => f.Name.EndsWith(".debug.log", StringComparison.Ordinal))
            .Which.ReadAllText()
            .Should()
            .Contain("debug event")
            .And.NotContain("verbose event");
    }

    [Test]
    public void Entry_after_an_exception_starts_on_its_own_line()
    {
        var paths = CreatePaths();

        WriteLogs(
            paths,
            log =>
            {
                log.Debug(CreateThrownException(), "failing event");
                log.Debug("next event");
            }
        );

        paths
            .ServerLogDirectory.GetFiles("*.debug.log")
            .Should()
            .ContainSingle()
            .Which.ReadAllLines()
            .Should()
            .ContainMatch("[* DBG] next event");
    }

    [TestCase(ServerMode.Persistent)]
    [TestCase(ServerMode.Ephemeral)]
    public void Log_files_are_written_by_default(ServerMode mode)
    {
        var paths = CreatePaths();

        WriteLogs(paths, log => log.Information("event"), mode, new ServerLoggingSettings());

        paths.ServerLogDirectory.GetFiles().Should().HaveCount(2);
    }

    [Test]
    public void Log_files_can_be_turned_off()
    {
        var paths = CreatePaths();

        WriteLogs(
            paths,
            log => log.Information("event"),
            logging: new ServerLoggingSettings { Files = false }
        );

        paths.ServerLogDirectory.GetFiles().Should().BeEmpty();
    }

    private AppPaths CreatePaths()
    {
        var paths = new AppPaths(
            _root.SubDirectory("config"),
            _root.SubDirectory("data"),
            _root.SubDirectory("state")
        );
        paths.CreateWritableDirectories();
        return paths;
    }

    private static void WriteLogs(
        IAppPaths paths,
        Action<ILogger> write,
        ServerMode mode = ServerMode.Persistent,
        ServerLoggingSettings? logging = null
    )
    {
        var settings = Substitute.For<ISettings<ServerSettings>>();
        settings.Value.Returns(new ServerSettings { Logging = logging ?? new() });

        using var serverLogger = new ServerLogger(
            paths,
            new LoggingLevelSwitch(LogEventLevel.Fatal),
            mode,
            new Lazy<ISettings<ServerSettings>>(settings),
            new ConfigurationBuilder().Build()
        );

        serverLogger.Reload();
        write(serverLogger);
    }

    private static InvalidOperationException CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException e)
        {
            return e;
        }
    }
}
