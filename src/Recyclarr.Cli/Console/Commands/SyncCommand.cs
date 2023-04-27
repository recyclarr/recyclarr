using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Sync the guide to services")]
[UsedImplicitly]
public class SyncCommand : AsyncCommand<SyncCommand.CliSettings>
{
    private readonly IMigrationExecutor _migration;
    private readonly IRepoUpdater _repoUpdater;
    private readonly ISyncProcessor _syncProcessor;

    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it")]
    public class CliSettings : ServiceCommandSettings, ISyncSettings
    {
        [CommandArgument(0, "[service]")]
        [EnumDescription<SupportedServices>("The service to sync. If not specified, all services are synced.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices? Service { get; init; }

        [CommandOption("-c|--config")]
        [Description("One or more YAML configuration files to load & use.")]
        [TypeConverter(typeof(FileInfoConverter))]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public IFileInfo[] ConfigsOption { get; init; } = Array.Empty<IFileInfo>();
        public IReadOnlyCollection<IFileInfo> Configs => ConfigsOption;

        [CommandOption("-p|--preview")]
        [Description("Perform a dry run: preview the results without syncing.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-i|--instance")]
        [Description("One or more instance names to sync. If not specified, all instances will be synced.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] InstancesOption { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Instances => InstancesOption;
    }

    public SyncCommand(
        IMigrationExecutor migration,
        IRepoUpdater repoUpdater,
        ISyncProcessor syncProcessor)
    {
        _migration = migration;
        _repoUpdater = repoUpdater;
        _syncProcessor = syncProcessor;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        // Will throw if migration is required, otherwise just a warning is issued.
        _migration.CheckNeededMigrations();
        await _repoUpdater.UpdateRepo();

        return (int) await _syncProcessor.ProcessConfigs(settings);
    }
}
