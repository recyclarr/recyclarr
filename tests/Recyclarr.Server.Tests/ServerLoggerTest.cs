using System.IO.Abstractions;
using Recyclarr.Platform;
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

    private AppPaths CreatePaths()
    {
        var paths = new AppPaths(_root.SubDirectory("config"), _root.SubDirectory("data"));
        paths.CreateTopDirectories();
        return paths;
    }

    private static void WriteLogs(IAppPaths paths, Action<ILogger> write)
    {
        var serverLogger = new ServerLogger(
            paths,
            new LoggingLevelSwitch(LogEventLevel.Fatal),
            new ServerLogOptions(LogEventLevel.Fatal, UseParentProtocol: false)
        );

        var config = new LoggerConfiguration();
        serverLogger.Configure(config);
        using var log = config.CreateLogger();
        write(log);
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
