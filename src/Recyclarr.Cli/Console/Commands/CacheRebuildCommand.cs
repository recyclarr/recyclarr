using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Migration;
using Recyclarr.Cli.Processors.CacheRebuild;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Rebuild cache by matching guide resources to service resources")]
[UsedImplicitly]
internal class CacheRebuildCommand(
    MigrationExecutor migration,
    ProviderProgressHandler providerProgressHandler,
    CacheRebuildProcessor processor,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<CacheRebuildCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings, ICacheRebuildSettings
    {
        [CommandArgument(position: 0, "[resource]")]
        [EnumDescription<CacheableResourceType>(
            "The resource type to rebuild cache for. If not specified, all resource types are rebuilt."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public CacheableResourceType? Resource { get; init; }

        [CommandOption("-i|--instance")]
        [Description(
            "One or more instance names to rebuild cache for. If not specified, all instances are processed."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] InstancesOption { get; init; } = [];
        public IReadOnlyCollection<string> Instances => InstancesOption;

        [CommandOption("-p|--preview")]
        [Description("Preview cache rebuild without saving changes.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Show detailed information including cache file paths and per-item state.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Verbose { get; init; }

        [CommandOption("--adopt")]
        [Description(
            "Take ownership of existing custom formats that match by name. "
                + "Use this to let Recyclarr manage CFs you previously created manually."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Adopt { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        migration.CheckNeededMigrations();

        var outputSettings = consoleSettings.GetOutputSettings(settings);
        await providerProgressHandler.InitializeProvidersAsync(outputSettings, ct);

        return (int)await processor.Process(settings, ct);
    }
}
