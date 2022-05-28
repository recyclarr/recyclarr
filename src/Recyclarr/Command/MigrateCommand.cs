using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Recyclarr.Command.Initialization;
using Recyclarr.Migration;

namespace Recyclarr.Command;

[Command("migrate", Description = "Perform any migration steps that may be needed between versions")]
[UsedImplicitly]
public class MigrateCommand : ICommand
{
    private readonly IMigrationExecutor _migration;
    private readonly IDefaultAppDataSetup _appDataSetup;

    public MigrateCommand(IMigrationExecutor migration, IDefaultAppDataSetup appDataSetup)
    {
        _migration = migration;
        _appDataSetup = appDataSetup;
    }

    public ValueTask ExecuteAsync(IConsole console)
    {
        _appDataSetup.SetupDefaultPath();
        PerformMigrations();
        return ValueTask.CompletedTask;
    }

    private void PerformMigrations()
    {
        try
        {
            _migration.PerformAllMigrationSteps();
        }
        catch (MigrationException e)
        {
            var msg = new StringBuilder();
            msg.AppendLine("Fatal exception during migration step. Details are below.\n");
            msg.AppendLine($"Step That Failed:  {e.OperationDescription}");
            msg.AppendLine($"Failure Reason:    {e.OriginalException.Message}");

            if (e.Remediation.Any())
            {
                msg.AppendLine("\nPossible remediation steps:");
                foreach (var remedy in e.Remediation)
                {
                    msg.AppendLine($" - {remedy}");
                }
            }

            throw new CommandException(msg.ToString());
        }
    }
}
