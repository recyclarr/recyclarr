using Recyclarr.Cli.Console.Commands;
using Recyclarr.Repo;

namespace Recyclarr.Cli.Tests.Console.Commands;

[TestFixture]
public class ConfigCommandsTest
{
    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_config_list(
        [Frozen] IMultiRepoUpdater updater,
        ConfigListLocalCommand sut)
    {
        await sut.ExecuteAsync(default!, new ConfigListLocalCommand.CliSettings());

        await updater.ReceivedWithAnyArgs().UpdateAllRepositories(default);
    }

    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_config_create(
        [Frozen] IMultiRepoUpdater updater,
        ConfigCreateCommand sut)
    {
        await sut.ExecuteAsync(default!, new ConfigCreateCommand.CliSettings());

        await updater.ReceivedWithAnyArgs().UpdateAllRepositories(default);
    }
}
