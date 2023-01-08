using Spectre.Console;

namespace Recyclarr.Cli.Migration;

public class MigrationExecutor : IMigrationExecutor
{
    private readonly IAnsiConsole _console;
    private readonly List<IMigrationStep> _migrationSteps;

    public MigrationExecutor(IEnumerable<IMigrationStep> migrationSteps, IAnsiConsole console)
    {
        _console = console;
        _migrationSteps = migrationSteps.OrderBy(x => x.Order).ToList();
    }

    public void PerformAllMigrationSteps(bool withDiagnostics)
    {
        _console.WriteLine("Performing migration steps...");

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
                step.Execute(withDiagnostics ? _console : null);
            }
            catch (Exception e) when (e is not MigrationException)
            {
                throw new MigrationException(e, step.Description, step.Remediation);
            }

            _console.WriteLine($"Migrate: {step.Description}");
        }
    }

    public void CheckNeededMigrations()
    {
        var neededMigrationSteps = _migrationSteps.Where(x => x.CheckIfNeeded()).ToList();
        if (neededMigrationSteps.Count == 0)
        {
            return;
        }

        var wereAnyRequired = false;

        foreach (var step in neededMigrationSteps)
        {
            var requiredText = step.Required ? "Required" : "Not Required";
            _console.WriteLine($"Migration Needed ({requiredText}): {step.Description}");
            wereAnyRequired |= step.Required;
        }

        _console.WriteLine(
            "\nRun the `migrate` subcommand to perform the above migration steps automatically\n");

        if (wereAnyRequired)
        {
            throw new RequiredMigrationException();
        }
    }
}
