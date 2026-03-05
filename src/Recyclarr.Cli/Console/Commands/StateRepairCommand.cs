using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.StateRepair;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Repair state by matching guide resources to service resources")]
[UsedImplicitly]
internal class StateRepairCommand(
    IAnsiConsole console,
    ILogger log,
    ProviderProgressHandler providerProgressHandler,
    ConfigurationRegistry configRegistry,
    InstanceScopeFactory instanceScopeFactory,
    ExceptionHandler exceptionHandler
) : AsyncCommand<StateRepairCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings, IStateRepairSettings
    {
        [CommandArgument(position: 0, "[resource]")]
        [EnumDescription<StatefulResourceType>(
            "The resource type to repair state for. If not specified, all resource types are repaired."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public StatefulResourceType? Resource { get; init; }

        [CommandOption("-i|--instance")]
        [Description(
            "One or more instance names to repair state for. If not specified, all instances are processed."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] InstancesOption { get; init; } = [];
        public IReadOnlyCollection<string> Instances => InstancesOption;

        [CommandOption("-p|--preview")]
        [Description("Preview state repair without saving changes.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Show detailed information including state file paths and per-item state.")]
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(silent: false, ct);

        var result = configRegistry.FindAndLoadConfigs(
            new ConfigFilterCriteria { Instances = settings.Instances }
        );

        ConfigFailureRenderer.Render(console, log, result);

        if (result.Configs.Count == 0)
        {
            console.MarkupLine("[yellow]No configurations found.[/]");
            return (int)ExitStatus.Succeeded;
        }

        var succeeded = 0;
        var failed = 0;

        foreach (var config in result.Configs)
        {
            try
            {
                using var scope = instanceScopeFactory.Start<StateRepairInstanceProcessor>(config);
                if (await scope.Entry.ProcessAsync(settings, ct))
                {
                    succeeded++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception e)
            {
                if (!await exceptionHandler.TryHandleAsync(e))
                {
                    throw;
                }

                failed++;
            }
        }

        console.WriteLine();
        console.Write(
            new Rule($"[bold]State repair: {succeeded} succeeded, {failed} failed[/]").RuleStyle(
                "dim"
            )
        );

        return (int)(failed > 0 ? ExitStatus.Failed : ExitStatus.Succeeded);
    }
}
