using Recyclarr.Cli.Migration.Steps;
using Spectre.Console;

namespace Recyclarr.Cli.Migration;

public class MigrationExecutor(IOrderedEnumerable<IMigrationStep> migrationSteps, IAnsiConsole console)
    : IMigrationExecutor
{
    public void PerformAllMigrationSteps(bool withDiagnostics)
    {
        foreach (var step in migrationSteps)
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
                step.Execute(withDiagnostics ? console : null);
            }
            catch (Exception e) when (e is not MigrationException)
            {
                throw new MigrationException(e, step.Description, step.Remediation);
            }

            console.WriteLine($"Migrate: {step.Description}");
        }
    }

    public void CheckNeededMigrations()
    {
        var neededMigrationSteps = migrationSteps.Where(x => x.CheckIfNeeded()).ToList();
        if (neededMigrationSteps.Count == 0)
        {
            return;
        }

        var wereAnyRequired = false;

        foreach (var step in neededMigrationSteps)
        {
            var requiredText = step.Required ? "Required" : "Not Required";
            console.WriteLine($"Migration Needed ({requiredText}): {step.Description}");
            wereAnyRequired |= step.Required;
        }

        console.WriteLine(
            "\nRun the `migrate` subcommand to perform the above migration steps automatically\n");

        if (wereAnyRequired)
        {
            throw new RequiredMigrationException();
        }
    }
}
