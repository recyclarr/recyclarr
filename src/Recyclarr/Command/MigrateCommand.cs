using System.Text;
using CliFx.Attributes;
using CliFx.Exceptions;
using JetBrains.Annotations;
using Recyclarr.Migration;

namespace Recyclarr.Command;

[Command("migrate", Description = "Perform any migration steps that may be needed between versions")]
[UsedImplicitly]
public class MigrateCommand : BaseCommand
{
    [CommandOption("app-data", Description =
        "Explicitly specify the location of the recyclarr application data directory. " +
        "Mainly for usage in Docker; not recommended for normal use.")]
    public override string? AppDataDirectory { get; set; }

    public override Task Process(IServiceLocatorProxy container)
    {
        var migration = container.Resolve<IMigrationExecutor>();

        try
        {
            migration.PerformAllMigrationSteps(Debug);
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

            throw new CommandException(msg.ToString());
        }

        return Task.CompletedTask;
    }
}
