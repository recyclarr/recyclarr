using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

#pragma warning disable CS8765
[UsedImplicitly]
[Description("List custom format groups available in the guide.")]
internal class ListCustomFormatGroupsCommand(
    ILogger log,
    IAnsiConsole console,
    CfGroupResourceQuery cfGroupQuery,
    ProviderProgressHandler providerProgressHandler
) : AsyncCommand<ListCustomFormatGroupsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    internal class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<service_type>")]
        [EnumDescription<SupportedServices>("The service type to obtain information about.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices Service { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(ct);

        console.WriteLine();
        console.WriteLine("List of Custom Format Groups in the TRaSH Guides:");
        console.WriteLine();

        var groups = cfGroupQuery.Get(settings.Service).OrderBy(g => g.Name).ToList();

        log.Debug(
            "Found {Count} custom format groups for {Service}",
            groups.Count,
            settings.Service
        );
        log.Information("Custom format groups: {@Groups}", groups.Select(g => g.TrashId));

        // Indentation matches YAML config structure for direct copy-paste into custom_format_groups
        foreach (var group in groups)
        {
            var cfCount = group.CustomFormats.Count;
            console.WriteLine($"          - {group.TrashId} # {group.Name} ({cfCount} CFs)");
        }

        console.WriteLine();
        console.WriteLine(
            "The above Custom Format Groups are in YAML format and ready to be copied & pasted "
                + "under the `custom_format_groups:` property."
        );

        return (int)ExitStatus.Succeeded;
    }
}
