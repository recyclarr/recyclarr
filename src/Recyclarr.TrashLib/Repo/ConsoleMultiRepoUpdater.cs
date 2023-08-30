using Spectre.Console;

namespace Recyclarr.TrashLib.Repo;

public class ConsoleMultiRepoUpdater : IMultiRepoUpdater
{
    private readonly IAnsiConsole _console;
    private readonly IReadOnlyCollection<IUpdateableRepo> _repos;

    public ConsoleMultiRepoUpdater(IAnsiConsole console, IReadOnlyCollection<IUpdateableRepo> repos)
    {
        _console = console;
        _repos = repos;
    }

    public async Task UpdateAllRepositories(CancellationToken token)
    {
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = 3
        };

        await _console.Status().StartAsync("Updating Git Repositories...", async _ =>
        {
            await Parallel.ForEachAsync(_repos, options, async (repo, innerToken) => await repo.Update(innerToken));
        });
    }
}
