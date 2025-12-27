using Recyclarr.Cli.Migration.Steps;
using Spectre.Console;

namespace Recyclarr.Cli.Migration;

internal class MigrationExecutor(
    IOrderedEnumerable<IMigrationStep> migrationSteps,
    IAnsiConsole console,
    ILogger log
)
{
    public void PerformAllMigrationSteps()
    {
        foreach (var step in migrationSteps)
        {
            // Do not use LINQ to filter using CheckIfNeeded(). If it returns true, then 'Execute()' must be invoked to
            // cause the necessary changes to happen. Those changes may be required in order for the *next* step's
            // CheckIfNeeded() to work properly!
            if (!step.CheckIfNeeded())
            {
                log.Debug("Migration step not needed: {Description}", step.Description);
                continue;
            }

            log.Debug("Executing migration step: {Description}", step.Description);

            try
            {
                step.Execute(log);
            }
            catch (Exception e) when (e is not MigrationException)
            {
                throw new MigrationException(e, step.Description, step.Remediation);
            }

            log.Information("Migration step completed: {Description}", step.Description);
            console.MarkupLineInterpolated($"[dim]Migrate:[/] {step.Description}");
        }
    }

    public void CheckNeededMigrations()
    {
        var neededMigrationSteps = migrationSteps.Where(x => x.CheckIfNeeded()).ToList();
        if (neededMigrationSteps.Count == 0)
        {
            log.Debug("No migrations needed");
            return;
        }

        log.Debug("Found {Count} migration(s) needed", neededMigrationSteps.Count);

        foreach (var step in neededMigrationSteps)
        {
            var requiredText = step.Required ? "Required" : "Not Required";
            log.Information(
                "Migration needed ({Required}): {Description}",
                requiredText,
                step.Description
            );

            if (step.Required)
            {
                console.MarkupLineInterpolated(
                    $"[yellow]Migration Needed (Required):[/] {step.Description}"
                );
            }
            else
            {
                console.MarkupLineInterpolated(
                    $"[dim]Migration Needed (Not Required):[/] {step.Description}"
                );
            }
        }

        console.WriteLine();
        console.MarkupLine(
            "[dim]Run the[/] [blue]migrate[/] [dim]subcommand to perform the above migration steps automatically[/]"
        );
        console.WriteLine();

        if (neededMigrationSteps.Exists(x => x.Required))
        {
            throw new RequiredMigrationException();
        }
    }
}
