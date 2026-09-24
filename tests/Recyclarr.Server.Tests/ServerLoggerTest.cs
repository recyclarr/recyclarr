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
        var paths = new AppPaths(_root.SubDirectory("config"), _root.SubDirectory("data"));
        paths.CreateTopDirectories();
        var serverLogger = new ServerLogger(
            paths,
            new LoggingLevelSwitch(LogEventLevel.Fatal),
            new ServerLogOptions(LogEventLevel.Fatal, UseParentProtocol: false)
        );

        var config = new LoggerConfiguration();
        serverLogger.Configure(config);
        using (var log = config.CreateLogger())
        {
            log.Verbose("verbose event");
            log.Debug("debug event");
        }

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
}
