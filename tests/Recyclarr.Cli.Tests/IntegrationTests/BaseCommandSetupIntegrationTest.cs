using System.IO.Abstractions;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class BaseCommandSetupIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Log_janitor_cleans_up_user_specified_max_files()
    {
        const int maxFiles = 25;

        Fs.AddFile(
            Paths.AppDataDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                log_janitor:
                  max_files: {maxFiles}
                """
            )
        );

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            Fs.AddFile(Paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        var sut = Resolve<JanitorCleanupTask>();
        sut.OnFinish();

        Fs.AllFiles.Where(x => x.StartsWith(Paths.LogDirectory.FullName, StringComparison.Ordinal))
            .Should()
            .HaveCount(maxFiles);
    }

    [Test]
    public void Log_janitor_cleans_up_default_max_files()
    {
        var maxFiles = Resolve<ISettings<LogJanitorSettings>>().Value.MaxFiles;

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            Fs.AddFile(Paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        var sut = Resolve<JanitorCleanupTask>();
        sut.OnFinish();

        maxFiles.Should().BePositive();
        Fs.AllFiles.Where(x => x.StartsWith(Paths.LogDirectory.FullName, StringComparison.Ordinal))
            .Should()
            .HaveCount(maxFiles);
    }
}
