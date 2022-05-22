using System.Text;
using CliFx.Exceptions;
using Recyclarr.Migration;

namespace Recyclarr.Command.Initialization.Init;

internal class ServicePreInitializer : IServiceInitializer
{
    private readonly IMigrationExecutor _migration;

    public ServicePreInitializer(IMigrationExecutor migration)
    {
        _migration = migration;
    }

    public void Initialize(ServiceCommand cmd)
    {
        // Migrations are performed before we process command line arguments because we cannot instantiate any service
        // objects via the DI container before migration logic is performed. This is due to the fact that migration
        // steps may alter important files and directories which those services may depend on.
        PerformMigrations();
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
