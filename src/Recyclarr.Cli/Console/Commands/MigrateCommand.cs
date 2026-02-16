using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Perform migration steps that may be needed between versions")]
internal class MigrateCommand(IAnsiConsole console, ILogger log, MigrationExecutor migration)
    : Command<MigrateCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings;

    public override int Execute(CommandContext context, CliSettings settings, CancellationToken ct)
    {
        try
        {
            migration.PerformAllMigrationSteps();
            log.Information("All migration steps completed");
            console.MarkupLine("[green]All migration steps completed[/]");
            return (int)ExitStatus.Succeeded;
        }
        catch (MigrationException e)
        {
            log.Warning(
                e.OriginalException,
                "Migration step failed: {Step}",
                e.OperationDescription
            );

            console.MarkupLine("[red]Error:[/] Fatal exception during migration step");
            console.MarkupLineInterpolated($"[dim]Step:[/] {e.OperationDescription}");
            console.MarkupLineInterpolated($"[dim]Reason:[/] {e.OriginalException.Message}");

            if (e.Remediation.Count != 0)
            {
                console.WriteLine();
                console.MarkupLine("[dim]Possible remediation steps:[/]");
                foreach (var remedy in e.Remediation)
                {
                    console.MarkupLineInterpolated($"  - {remedy}");
                }
            }
        }
        catch (RequiredMigrationException ex)
        {
            log.Error("Required migrations did not pass");
            console.MarkupLineInterpolated($"[red]Error:[/] {ex.Message}");
        }

        return (int)ExitStatus.Failed;
    }
}
