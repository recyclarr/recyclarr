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
            "recyclarr-server_2026-01-02_00-00-00.debug.log",
            "recyclarr-server_2026-01-03_00-00-00.debug.log",
        ];
        var logs = logNames.Select(paths.ServerLogDirectory.File).ToList();

        foreach (var log in logs)
        {
            fs.AddEmptyFile(log);
        }

        var settings = Substitute.For<ISettings<LogJanitorSettings>>();
        settings.Value.Returns(new LogJanitorSettings { MaxFiles = 2 });

        new ServerLogJanitor(paths, settings).DeleteOldestLogFiles(logs[2]);

        fs.AllFiles.Should().BeEquivalentTo(logs[1].FullName, logs[2].FullName);
    }

    [Test]
    public void Active_log_is_retained_when_max_files_is_zero()
    {
        var fs = new MockFileSystem();
        var root = fs.CurrentDirectory();
        var paths = new AppPaths(root.SubDirectory("config"), root.SubDirectory("data"));
        var oldLog = paths.ServerLogDirectory.File("recyclarr-server_old.debug.log");
        var activeLog = paths.ServerLogDirectory.File("recyclarr-server_active.debug.log");
        fs.AddEmptyFile(oldLog);
        fs.AddEmptyFile(activeLog);
        var settings = Substitute.For<ISettings<LogJanitorSettings>>();
        settings.Value.Returns(new LogJanitorSettings { MaxFiles = 0 });

        new ServerLogJanitor(paths, settings).DeleteOldestLogFiles(activeLog);

        fs.AllFiles.Should().BeEquivalentTo(activeLog.FullName);
    }
}
