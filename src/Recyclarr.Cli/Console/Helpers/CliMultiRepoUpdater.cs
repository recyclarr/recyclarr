using Recyclarr.Repo;

namespace Recyclarr.Cli.Console.Helpers;

public class CliMultiRepoUpdater(
    IMultiRepoUpdater repoUpdater,
    IApplicationCancellationTokenProvider cancellationTokenProvider)
{
    public async Task UpdateAllRepositories()
    {
        await repoUpdater.UpdateAllRepositories(cancellationTokenProvider.Token);
    }

    public async Task UpdateAllRepositories(bool hideConsoleOutput)
    {
        await repoUpdater.UpdateAllRepositories(cancellationTokenProvider.Token, hideConsoleOutput);
    }
}
