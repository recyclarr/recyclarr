using Spectre.Console;

namespace Recyclarr.Repo;

public class ConsoleMultiRepoUpdater(
    IAnsiConsole console,
    IReadOnlyCollection<IUpdateableRepo> repos
) : IMultiRepoUpdater
{
    public async Task UpdateAllRepositories(CancellationToken token, bool hideConsoleOutput = false)
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
