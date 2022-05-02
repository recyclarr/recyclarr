using CliFx.Infrastructure;

namespace Recyclarr.Migration;

public class MigrationExecutor : IMigrationExecutor
{
    private readonly IConsole _console;
    private readonly List<IMigrationStep> _migrationSteps;

    public MigrationExecutor(IEnumerable<IMigrationStep> migrationSteps, IConsole console)
    {
        _console = console;
        _migrationSteps = migrationSteps.OrderBy(x => x.Order).ToList();
    }

    public void PerformAllMigrationSteps()
    {
        _console.Output.WriteLine("Performing migration steps...");

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var step in _migrationSteps)
        {
            // Do not use LINQ to filter using CheckIfNeeded(). If it returns true, then 'Execute()' must be invoked to
            // cause the necessary changes to happen. Those changes may be required in order for the *next* step's
            // CheckIfNeeded() to work properly!
            if (!step.CheckIfNeeded())
            {
                continue;
            }

            try
            {
                step.Execute();
            }
            catch (Exception e) when (e is not MigrationException)
            {
                throw new MigrationException(e, step.Description, step.Remediation);
            }

            _console.Output.WriteLine($"Migrate: {step.Description}");
        }
    }
}
