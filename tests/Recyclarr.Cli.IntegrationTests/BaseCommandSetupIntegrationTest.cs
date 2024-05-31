using System.IO.Abstractions;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Settings;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
internal class BaseCommandSetupIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Base_command_startup_tasks_are_registered()
    {
        var registrations = Resolve<IEnumerable<IGlobalSetupTask>>();
        registrations.Select(x => x.GetType()).Should().BeEquivalentTo(new[]
        {
            typeof(JanitorCleanupTask)
        });
    }

    [Test]
    public void Log_janitor_cleans_up_user_specified_max_files()
    {
        const int maxFiles = 25;

        Fs.AddFile(Paths.AppDataDirectory.File("settings.yml").FullName, new MockFileData(
            $"""
             log_janitor:
               max_files: {maxFiles}
             """));

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            Fs.AddFile(Paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        var sut = Resolve<JanitorCleanupTask>();
        sut.OnFinish();

        Fs.AllFiles.Where(x => x.StartsWith(Paths.LogDirectory.FullName))
            .Should().HaveCount(maxFiles);
    }

    [Test]
    public void Log_janitor_cleans_up_default_max_files()
    {
        var settingsProvider = Resolve<ISettingsProvider>();
        var maxFiles = settingsProvider.Settings.LogJanitor.MaxFiles;

        for (var i = 0; i < maxFiles + 20; ++i)
        {
            Fs.AddFile(Paths.LogDirectory.File($"logfile-{i}.log").FullName, new MockFileData(""));
        }

        var sut = Resolve<JanitorCleanupTask>();
        sut.OnFinish();

        maxFiles.Should().BePositive();
        Fs.AllFiles.Where(x => x.StartsWith(Paths.LogDirectory.FullName))
            .Should().HaveCount(maxFiles);
    }
}
