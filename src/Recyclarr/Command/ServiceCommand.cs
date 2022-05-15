using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Recyclarr.Migration;

namespace Recyclarr.Command;

public abstract class ServiceCommand : ICommand, IServiceCommand
{
    private readonly IMigrationExecutor _migration;

    [CommandOption("preview", 'p', Description =
        "Only display the processed markdown results without making any API calls.")]
    public bool Preview { get; [UsedImplicitly] set; } = false;

    [CommandOption("debug", 'd', Description =
        "Display additional logs useful for development/debug purposes.")]
    public bool Debug { get; [UsedImplicitly] set; } = false;

    [CommandOption("config", 'c', Description =
        "One or more YAML config files to use. All configs will be used and settings are additive. " +
        "If not specified, the script will look for `recyclarr.yml` in the same directory as the executable.")]
    public ICollection<string> Config { get; [UsedImplicitly] set; } =
        new List<string> {AppPaths.DefaultConfigPath};

    public abstract string CacheStoragePath { get; }
    public abstract string Name { get; }

    protected ServiceCommand(IMigrationExecutor migration)
    {
        _migration = migration;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // Stuff that needs to happen pre-service-initialization goes here

        // Migrations are performed before we process command line arguments because we cannot instantiate any service
        // objects via the DI container before migration logic is performed. This is due to the fact that migration
        // steps may alter important files and directories which those services may depend on.
        PerformMigrations();

        // Initialize command services and execute business logic (system environment changes should be done by this
        // point)
        await Process();
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

    protected abstract Task Process();
}
