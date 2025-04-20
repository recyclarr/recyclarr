using Recyclarr.Repo;
using Spectre.Console;

namespace Recyclarr.Cli.Console;

internal class ConsoleMultiRepoUpdater(
    IAnsiConsole console,
    IReadOnlyCollection<IUpdateableRepo> repos
) : IMultiRepoUpdater
{
    public async Task UpdateAllRepositories(bool hideConsoleOutput, CancellationToken token)
    {
        var options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 3 };

        var task = Parallel.ForEachAsync(
            repos,
            options,
            async (repo, innerToken) => await repo.Update(innerToken)
        );

        if (!hideConsoleOutput)
        {
            await console.Status().StartAsync("Updating Git Repositories...", _ => task);
        }
        else
        {
            await task;
        }
    }
}
