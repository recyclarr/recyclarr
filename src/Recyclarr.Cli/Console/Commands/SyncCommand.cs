using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Repo;
using Recyclarr.TrashGuide;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Sync the guide to services")]
[UsedImplicitly]
public class SyncCommand(IMigrationExecutor migration, IMultiRepoUpdater repoUpdater, ISyncProcessor syncProcessor)
    : AsyncCommand<SyncCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it")]
    public class CliSettings : BaseCommandSettings, ISyncSettings
    {
        [CommandArgument(0, "[service]")]
        [EnumDescription<SupportedServices>("The service to sync. If not specified, all services are synced.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices? Service { get; init; }

        [CommandOption("-c|--config")]
        [Description("One or more YAML configuration files to load & use.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] ConfigsOption { get; init; } = [];
        public IReadOnlyCollection<string> Configs => ConfigsOption;

        [CommandOption("-p|--preview")]
        [Description("Perform a dry run: preview the results without syncing.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-i|--instance")]
        [Description("One or more instance names to sync. If not specified, all instances will be synced.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] InstancesOption { get; init; } = [];
        public IReadOnlyCollection<string> Instances => InstancesOption;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        // Will throw if migration is required, otherwise just a warning is issued.
        migration.CheckNeededMigrations();

        await repoUpdater.UpdateAllRepositories(settings.CancellationToken);

        return (int) await syncProcessor.Process(settings, settings.CancellationToken);
    }
}
