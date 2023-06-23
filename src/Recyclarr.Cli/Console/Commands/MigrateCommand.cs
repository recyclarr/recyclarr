using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;
using Recyclarr.Cli.Migration;
using Spectre.Console;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("Perform migration steps that may be needed between versions")]
public class MigrateCommand : Command<MigrateCommand.CliSettings>
{
    private readonly IMigrationExecutor _migration;
    private readonly IAnsiConsole _console;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : ServiceCommandSettings
    {
    }

    public MigrateCommand(
        IAnsiConsole console,
        IMigrationExecutor migration)
    {
        _console = console;
        _migration = migration;
    }

    public override int Execute(CommandContext context, CliSettings settings)
    {
        try
        {
            _migration.PerformAllMigrationSteps(settings.Debug);
            _console.WriteLine("All migration steps completed");
        }
        catch (MigrationException e)
        {
            var msg = new StringBuilder();
            msg.AppendLine("Fatal exception during migration step. Details are below.\n");
            msg.AppendLine($"Step That Failed:  {e.OperationDescription}");
            msg.AppendLine($"Failure Reason:    {e.OriginalException.Message}");

            // ReSharper disable once InvertIf
            if (e.Remediation.Any())
            {
                msg.AppendLine("\nPossible remediation steps:");
                foreach (var remedy in e.Remediation)
                {
                    msg.AppendLine($" - {remedy}");
                }
            }

            _console.Write(msg.ToString());
            return 1;
        }
        catch (RequiredMigrationException ex)
        {
            _console.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }

        return 0;
    }
}
