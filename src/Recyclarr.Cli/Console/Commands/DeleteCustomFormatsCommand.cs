using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Delete;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Delete things from services like Radarr and Sonarr")]
[UsedImplicitly]
public class DeleteCustomFormatsCommand(IDeleteCustomFormatsProcessor processor)
    : AsyncCommand<DeleteCustomFormatsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it")]
    public class CliSettings : BaseCommandSettings, IDeleteCustomFormatSettings
    {
        [CommandArgument(0, "<instance_name>")]
        [Description("The name of the instance to delete CFs from.")]
        public string InstanceName { get; init; } = "";

        [CommandArgument(0, "[cf_names]")]
        [Description("One or more custom format names to delete. Optional only if `--all` is used.")]
        public string[] CustomFormatNamesOption { get; init; } = [];
        public IReadOnlyCollection<string> CustomFormatNames => CustomFormatNamesOption;

        [CommandOption("-a|--all")]
        [Description("Delete ALL custom formats.")]
        public bool All { get; init; } = false;

        [CommandOption("-f|--force")]
        [Description("Perform the delete operation with NO confirmation prompt.")]
        public bool Force { get; init; } = false;

        [CommandOption("-p|--preview")]
        [Description("Preview what custom formats will be deleted without actually deleting them.")]
        public bool Preview { get; init; } = false;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        await processor.Process(settings, settings.CancellationToken);
        return (int) ExitStatus.Succeeded;
    }
}
