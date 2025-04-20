using Recyclarr.Cli.Logging;
using Recyclarr.Repo;
using Spectre.Console;

namespace Recyclarr.Cli.Console;

internal class ConsoleMultiRepoUpdater(
    IAnsiConsole console,
    IReadOnlyCollection<IUpdateableRepo> repos
)
{
    public async Task UpdateAllRepositories(
        IConsoleOutputSettings outputSettings,
        CancellationToken token
    )
    {
        var options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 3 };

        var task = Parallel.ForEachAsync(
            repos,
            options,
            async (repo, innerToken) => await repo.Update(innerToken)
        );

        if (outputSettings.IsRawOutputEnabled)
        {
            await task;
            return;
        }

        await console.Status().StartAsync("Updating Git Repositories...", _ => task);
    }
}
