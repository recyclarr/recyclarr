using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Config;
using Recyclarr.TrashGuide;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Sync the guide to services")]
[UsedImplicitly]
internal class SyncCommand(
    ProviderProgressHandler providerProgressHandler,
    LifetimeScopeFactory scopeFactory
) : AsyncCommand<SyncCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings, ISyncSettings
    {
        [CommandArgument(0, "[service]")]
        [EnumDescription<SupportedServices>(
            "The service to sync. If not specified, all services are synced."
        )]
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
        [Description(
            "One or more instance names to sync. If not specified, all instances will be synced."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] InstancesOption { get; init; } = [];
        public IReadOnlyCollection<string> Instances => InstancesOption;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(silent: false, ct);

        // Sync scope owns the lifecycle of all sync-run-scoped services.
        // Disposing it fires OnCompleted on all observables, triggering
        // diagnostics rendering and notifications automatically.
        using var syncScope = scopeFactory.Start<SyncScope>("sync");
        return (int)await syncScope.Processor.Process(settings, ct);
    }
}
