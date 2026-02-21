using Autofac.Features.Metadata;
using Recyclarr.Cli.Migration.Steps;
using Spectre.Console;

namespace Recyclarr.Cli.Migration;

internal class MigrationExecutor(
    IEnumerable<Meta<IMigrationStep>> migrationSteps,
    IAnsiConsole console,
    ILogger log
)
{
    // Sort migration steps by Order metadata at resolution time
    private IEnumerable<IMigrationStep> MigrationSteps { get; } =
        migrationSteps
            .OrderBy(m =>
                m.Metadata.TryGetValue("Order", out var order)
                    ? order as int? ?? int.MaxValue
                    : int.MaxValue
            )
            .Select(m => m.Value)
            .ToList();

    public void PerformAllMigrationSteps()
    {
        foreach (var step in MigrationSteps)
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
}
