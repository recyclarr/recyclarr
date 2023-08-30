using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Tests.Console.Commands;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ListCommandsIntegrationTest : CliIntegrationFixture
{
    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_list_custom_formats(
        [Frozen] IMultiRepoUpdater updater,
        ListCustomFormatsCommand sut)
    {
        await sut.ExecuteAsync(default!, new ListCustomFormatsCommand.CliSettings());

        await updater.ReceivedWithAnyArgs().UpdateAllRepositories(default);
    }

    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_list_qualities(
        [Frozen] IMultiRepoUpdater updater,
        ListQualitiesCommand sut)
    {
        await sut.ExecuteAsync(default!, new ListQualitiesCommand.CliSettings());

        await updater.ReceivedWithAnyArgs().UpdateAllRepositories(default);
    }

    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_list_release_profiles(
        [Frozen] IMultiRepoUpdater updater,
        ListReleaseProfilesCommand sut)
    {
        await sut.ExecuteAsync(default!, new ListReleaseProfilesCommand.CliSettings());

        await updater.ReceivedWithAnyArgs().UpdateAllRepositories(default);
    }
}
