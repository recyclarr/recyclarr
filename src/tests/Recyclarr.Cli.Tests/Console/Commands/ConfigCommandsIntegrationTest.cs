using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Tests.Console.Commands;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigCommandsIntegrationTest : CliIntegrationFixture
{
    [Test, AutoMockData]
    public async Task Repo_update_is_called_on_config_list(
        [Frozen] IMultiRepoUpdater updater,
        ConfigListCommand sut)
    {
        await sut.ExecuteAsync(default!, new ConfigListCommand.CliSettings());

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
