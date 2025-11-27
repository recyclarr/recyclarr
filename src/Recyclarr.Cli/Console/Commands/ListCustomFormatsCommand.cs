using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Processors;
using Recyclarr.TrashGuide;
using Spectre.Console.Cli;

#pragma warning disable CS8765

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List custom formats in the guide for a particular service.")]
internal class ListCustomFormatsCommand(
    CustomFormatDataLister lister,
    ProviderProgressHandler providerProgressHandler,
    RecyclarrConsoleSettings consoleSettings
) : AsyncCommand<ListCustomFormatsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings, IListCustomFormatSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }

        [CommandOption("--score-sets")]
        [Description(
            "Instead of listing custom formats, list the score sets all custom formats are part of."
        )]
        public bool ScoreSets { get; init; } = false;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        var outputSettings = consoleSettings.GetOutputSettings(settings);
        await providerProgressHandler.InitializeProvidersAsync(outputSettings, ct);
        lister.List(outputSettings, settings);
        return (int)ExitStatus.Succeeded;
    }
}
