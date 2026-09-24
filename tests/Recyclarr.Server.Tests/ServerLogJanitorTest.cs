using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Tests;

internal sealed class ServerLogJanitorTest
{
    [Test]
    public void Keep_active_log_and_correct_number_of_newest_files()
    {
        var fs = new MockFileSystem();
        var root = fs.CurrentDirectory();
        var paths = new AppPaths(root.SubDirectory("config"), root.SubDirectory("data"));
        string[] logNames =
        [
            "recyclarr-server_2026-01-01_00-00-00.debug.log",
            "recyclarr-server_2026-01-01_00-00-00.verbose.log",
            "recyclarr-server_2026-01-02_00-00-00.debug.log",
            "recyclarr-server_2026-01-02_00-00-00.verbose.log",
            "recyclarr-server_2026-01-03_00-00-00.debug.log",
            "recyclarr-server_2026-01-03_00-00-00.verbose.log",
        ];
        var logs = logNames.Select(paths.ServerLogDirectory.File).ToList();

        foreach (var log in logs)
        {
            fs.AddEmptyFile(log);
        }

        var settings = Substitute.For<ISettings<LogJanitorSettings>>();
        settings.Value.Returns(new LogJanitorSettings { MaxFiles = 4 });

        new ServerLogJanitor(paths, settings).DeleteOldestLogFiles([logs[4], logs[5]]);

        fs.AllFiles.Should()
            .BeEquivalentTo(logs[2].FullName, logs[3].FullName, logs[4].FullName, logs[5].FullName);
    }

    [Test]
    public void Active_logs_are_retained_when_max_files_is_below_active_count()
    {
        var fs = new MockFileSystem();
        var root = fs.CurrentDirectory();
        var paths = new AppPaths(root.SubDirectory("config"), root.SubDirectory("data"));
        var oldLog = paths.ServerLogDirectory.File("recyclarr-server_old.debug.log");
        var activeDebugLog = paths.ServerLogDirectory.File("recyclarr-server_new.debug.log");
        var activeVerboseLog = paths.ServerLogDirectory.File("recyclarr-server_new.verbose.log");
        fs.AddEmptyFile(oldLog);
        fs.AddEmptyFile(activeDebugLog);
        fs.AddEmptyFile(activeVerboseLog);
        var settings = Substitute.For<ISettings<LogJanitorSettings>>();
        settings.Value.Returns(new LogJanitorSettings { MaxFiles = 1 });

        new ServerLogJanitor(paths, settings).DeleteOldestLogFiles([
            activeDebugLog,
            activeVerboseLog,
        ]);

        fs.AllFiles.Should().BeEquivalentTo(activeDebugLog.FullName, activeVerboseLog.FullName);
    }
}
