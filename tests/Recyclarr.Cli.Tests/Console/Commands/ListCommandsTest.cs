using Recyclarr.Cli.Console.Commands;
using Recyclarr.Repo;

namespace Recyclarr.Cli.Tests.Console.Commands;

[TestFixture]
public class ListCommandsTest
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
}
