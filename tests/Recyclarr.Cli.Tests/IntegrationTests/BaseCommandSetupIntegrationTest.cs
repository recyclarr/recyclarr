using System.IO.Abstractions;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Cli.Tests.IntegrationTests;

[CliDataSource]
internal sealed class BaseCommandSetupIntegrationTest(
    JanitorCleanupTask sut,
    ISettings<LogJanitorSettings> logJanitorSettings,
    MockFileSystem fs,
    IAppPaths paths
)
{
    [Test]
    public void Log_janitor_cleans_up_user_specified_max_files()
    {
        const int maxFiles = 25;

        fs.AddFile(
            paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                log_janitor:
                  max_files: {maxFiles}
                """
            )
        );

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            fs.AddFile(paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        sut.OnFinish();

        fs.AllFiles.Where(x => x.StartsWith(paths.LogDirectory.FullName, StringComparison.Ordinal))
            .Should()
            .HaveCount(maxFiles);
    }

    [Test]
    public void Log_janitor_cleans_up_default_max_files()
    {
        var maxFiles = logJanitorSettings.Value.MaxFiles;

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            fs.AddFile(paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        sut.OnFinish();

        maxFiles.Should().BePositive();
        fs.AllFiles.Where(x => x.StartsWith(paths.LogDirectory.FullName, StringComparison.Ordinal))
            .Should()
            .HaveCount(maxFiles);
    }
}
