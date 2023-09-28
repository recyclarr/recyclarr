using Spectre.Console;

namespace Recyclarr.Repo;

public class ConsoleMultiRepoUpdater : IMultiRepoUpdater
{
    private readonly IAnsiConsole _console;
    private readonly IReadOnlyCollection<IUpdateableRepo> _repos;

    public ConsoleMultiRepoUpdater(IAnsiConsole console, IReadOnlyCollection<IUpdateableRepo> repos)
    {
        _console = console;
        _repos = repos;
    }

    public async Task UpdateAllRepositories(CancellationToken token, bool hideConsoleOutput = false)
    {
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = 3
        };

        var task = Parallel.ForEachAsync(_repos, options, async (repo, innerToken) => await repo.Update(innerToken));

        if (!hideConsoleOutput)
        {
            await _console.Status().StartAsync("Updating Git Repositories...", _ => task);
        }
        else
        {
            await task;
        }
    }
}
