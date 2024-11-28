using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Perform migration steps that may be needed between versions")]
public class MigrateCommand(IAnsiConsole console, IMigrationExecutor migration)
    : Command<MigrateCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings;

    public override int Execute(CommandContext context, CliSettings settings)
    {
        try
        {
            migration.PerformAllMigrationSteps(settings.Debug);
            console.WriteLine("All migration steps completed");
            return (int)ExitStatus.Succeeded;
        }
        catch (MigrationException e)
        {
            var msg = new StringBuilder();
            msg.AppendLine("Fatal exception during migration step. Details are below.\n");
            msg.AppendLine(
                CultureInfo.InvariantCulture,
                $"Step That Failed:  {e.OperationDescription}"
            );
            msg.AppendLine(
                CultureInfo.InvariantCulture,
                $"Failure Reason:    {e.OriginalException.Message}"
            );

            // ReSharper disable once InvertIf
            if (e.Remediation.Count != 0)
            {
                msg.AppendLine("\nPossible remediation steps:");
                foreach (var remedy in e.Remediation)
                {
                    msg.AppendLine(CultureInfo.InvariantCulture, $" - {remedy}");
                }
            }

            console.Write(msg.ToString());
        }
        catch (RequiredMigrationException ex)
        {
            console.WriteLine($"ERROR: {ex.Message}");
        }

        return (int)ExitStatus.Failed;
    }
}
